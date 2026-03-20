using System.Collections.Generic;
using System.Linq;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Components.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ComputerGameFinal.Engine;

public class GameObject
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public Vector2 Scale { get; set; } = Vector2.One;

    public bool Active { get; set; } = true;
    public string Tag { get; set; }

    private readonly List<Component> _components = [];

    public virtual void Initialize() { }
    public virtual void Update(GameTime gameTime) { }

    // ---- Collision callbacks (Physics) ----
    public virtual void OnCollisionEnter2D(Collider collider) { }
    public virtual void OnCollisionStay2D(Collider collider) { }
    public virtual void OnCollisionExit2D(Collider collider) { }

    // ---- Trigger callbacks (IsTrigger = true) ----
    public virtual void OnTriggerEnter2D(Collider other) { }
    public virtual void OnTriggerStay2D(Collider other) { }
    public virtual void OnTriggerExit2D(Collider other) { }

    public T AddComponent<T>() where T : Component, new()
    {
        T component = new T();
        _components.Add(component);

        component.GameObject = this;
        component.Initialize();
        
        return component;
    }

    public T GetComponent<T>() where T : Component
    {
        return _components.OfType<T>().FirstOrDefault();
    }

    public IEnumerable<T> GetComponents<T>() where T : Component
    {
        return _components.OfType<T>();
    }

    public bool HasComponent<T>() where T : Component
    {
        return _components.OfType<T>().Any();
    }

    public void RemoveComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component != null)
        {
            _components.Remove(component);
        }
    }

    public void InitializeComponents()
    {
        foreach (var component in _components)
        {
            component.Initialize();
        }
    }

    public void UpdateComponents(GameTime gameTime)
    {
        if (!Active) return;
        foreach (var component in _components)
        {
            if (component.Enabled)
            {
                component.Update(gameTime);
            }   
        }
    }

    public void DrawComponents(SpriteBatch spriteBatch)
    {
        if (!Active) return;
        foreach (var component in _components)
        {
            if (component.Enabled)
            {
                component.Draw(spriteBatch);
            }
        }
    }
}