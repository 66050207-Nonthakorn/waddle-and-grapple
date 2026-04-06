using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Physics;

namespace WaddleAndGrapple.Engine;

public abstract class Scene
{
    public virtual bool IsPlayScene => false;

    protected Dictionary<string, GameObject> GameObjects { get; } = [];
    private readonly List<string> _deadObjects = [];
    private readonly List<(string name, GameObject go)> _pendingAdd = [];
    private bool _isUpdating;

    // Tracks which collider pairs were touching in the previous frame.
    private HashSet<ColliderPair> _previousCollisions = [];
    private HashSet<ColliderPair> _currentCollisions = [];
    
    public Camera2D Camera { get; set; }

    public IEnumerable<T> GetComponents<T>() where T : Component
        => GameObjects.Values
            .Where(go => go.Active)
            .SelectMany(go => go.GetComponents<T>().Where(c => c.Enabled));

    // For add game objects
    public abstract void Setup();
    
    public void Load()
    {
        this.Setup();

        foreach (var gameObject in GameObjects.Values)
        {
            gameObject.InitializeComponents();
        }

        foreach (var gameObject in GameObjects.Values)
        {
            gameObject.Initialize();
        }
    }

    public virtual void Unload()
    {
        GameObjects.Clear();
    }

    public virtual void Update(GameTime gameTime)
    {
        _isUpdating = true;

        foreach (var gameObject in GameObjects.Values)
        {
            gameObject.UpdateComponents(gameTime);
        }

        foreach (var gameObject in GameObjects.Values)
        {
            gameObject.Update(gameTime);
        }

        ProcessCollisions();

        _isUpdating = false;

        foreach (var name in _deadObjects)
        {
            GameObjects.Remove(name);
        }
        _deadObjects.Clear();

        // Flush objects spawned during this frame's update
        foreach (var (name, go) in _pendingAdd)
        {
            GameObjects.Add(name, go);
        }
        _pendingAdd.Clear();
    }

    // -------------------------------------------------------------------------
    // Collision detection & dispatch
    // -------------------------------------------------------------------------

    private void ProcessCollisions()
    {
        _currentCollisions.Clear();

        // Gather all active colliders across all GameObjects
        var colliders = GameObjects.Values
            .Where(go => go.Active)
            .SelectMany(go => go.GetComponents<Collider>().Where(c => c.Enabled))
            .ToList();

        // Test every unique pair
        for (int i = 0; i < colliders.Count; i++) {
            for (int j = i + 1; j < colliders.Count; j++)
            {
                var a = colliders[i];
                var b = colliders[j];

                if (!a.IsIntersect(b)) continue;

                if (!a.IsTrigger && !b.IsTrigger)
                {
                    ResolveOverlap(a, b);
                }
                
                _currentCollisions.Add(new ColliderPair(a, b));
            }
        }

        // --- Enter & Stay ---
        foreach (var pair in _currentCollisions)
        {
            bool isEnter = !_previousCollisions.Contains(pair);

            if (pair.A.IsTrigger || pair.B.IsTrigger)
            {
                // Trigger callbacks
                if (isEnter)
                {
                    pair.A.GameObject?.OnTriggerEnter2D(pair.B);
                    pair.B.GameObject?.OnTriggerEnter2D(pair.A);
                }
                else
                {
                    pair.A.GameObject?.OnTriggerStay2D(pair.B);
                    pair.B.GameObject?.OnTriggerStay2D(pair.A);
                }
            }
            else
            {
                // Collision callbacks
                if (isEnter)
                {
                    pair.A.GameObject?.OnCollisionEnter2D(pair.B);
                    pair.B.GameObject?.OnCollisionEnter2D(pair.A);
                }
                else
                {
                    pair.A.GameObject?.OnCollisionStay2D(pair.B);
                    pair.B.GameObject?.OnCollisionStay2D(pair.A);
                }
            }
        }

        // --- Exit ---
        foreach (var pair in _previousCollisions)
        {
            if (_currentCollisions.Contains(pair)) continue;

            if (pair.A.IsTrigger || pair.B.IsTrigger)
            {
                pair.A.GameObject?.OnTriggerExit2D(pair.B);
                pair.B.GameObject?.OnTriggerExit2D(pair.A);
            }
            else
            {
                pair.A.GameObject?.OnCollisionExit2D(pair.B);
                pair.B.GameObject?.OnCollisionExit2D(pair.A);
            }
        }

        // Swap for next frame
        // Swap sets to reuse allocations next frame
        (_currentCollisions, _previousCollisions) = (_previousCollisions, _currentCollisions);
    }

    // ── Depenetration solver ─────────────────────────────────────────────────
    private static void ResolveOverlap(Collider a, Collider b)
    {
        var rbA = a.GameObject?.GetComponent<Rigidbody2D>();
        var rbB = b.GameObject?.GetComponent<Rigidbody2D>();

        bool dynamicA = rbA != null && !rbA.IsKinematic;
        bool dynamicB = rbB != null && !rbB.IsKinematic;

        if (!dynamicA && !dynamicB) return;   // both static/kinematic — nothing to push

        // MTV points FROM b TOWARD a (pushes a out of b)
        Vector2 mtv = a.GetMTV(b);
        if (mtv == Vector2.Zero) return;

        Vector2 normal = Vector2.Normalize(mtv);
        float   depth  = mtv.Length();

        if (dynamicA && dynamicB)
        {
            // Split by inverse mass
            float totalInvMass = (1f / rbA.Mass) + (1f / rbB.Mass);
            float shareA = (1f / rbA.Mass) / totalInvMass;
            float shareB = (1f / rbB.Mass) / totalInvMass;
            rbA.ApplyDepenetration( normal * depth * shareA,  normal, rbB.Bounciness);
            rbB.ApplyDepenetration(-normal * depth * shareB, -normal, rbA.Bounciness);
        }
        else if (dynamicA)   // b is static/kinematic
        {
            rbA.ApplyDepenetration(mtv, normal, rbB?.Bounciness ?? 0f);
        }
        else                 // a is static/kinematic
        {
            rbB.ApplyDepenetration(-mtv, -normal, rbA?.Bounciness ?? 0f);
        }
    }

    // Canonical, order-independent collider pair for use in a HashSet.
    private readonly struct ColliderPair : IEquatable<ColliderPair>
    {
        public readonly Collider A;
        public readonly Collider B;

        public ColliderPair(Collider a, Collider b)
        {
            // Order by identity hash so (a,b) == (b,a)
            if (RuntimeHelpers.GetHashCode(a) <= RuntimeHelpers.GetHashCode(b))
            { A = a; B = b; }
            else
            { A = b; B = a; }
        }

        public bool Equals(ColliderPair other) => A == other.A && B == other.B;
        public override bool Equals(object obj) => obj is ColliderPair p && Equals(p);
        public override int GetHashCode() => HashCode.Combine(
            RuntimeHelpers.GetHashCode(A), RuntimeHelpers.GetHashCode(B));
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var gameObject in GameObjects.Values)
        {
            gameObject.DrawComponents(spriteBatch);
        }
    }
    
    public Matrix GetCameraTransform()
    {
        return Camera?.TransformMatrix ?? Matrix.Identity;
    }

    public T AddGameObject<T>(string name) where T : GameObject, new()
    {
        T gameObject = new T();

        if (_isUpdating)
            _pendingAdd.Add((name, gameObject));
        else
            GameObjects.Add(name, gameObject);

        return gameObject;
    }

    public T AddGameObject<T>(string name, T gameObject) where T : GameObject
    {
        GameObjects.Add(name, gameObject);
        return gameObject;
    }

    public void RemoveGameObject(string name)
    {
        if (GameObjects.ContainsKey(name))
        {
            _deadObjects.Add(name);
        }
    }
}