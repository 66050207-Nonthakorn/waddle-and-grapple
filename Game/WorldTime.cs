namespace WaddleAndGrapple.Game;

/// <summary>
/// Time scale ของ world — เมื่อ SlowTimePowerUp active ค่านี้จะน้อยกว่า 1
/// Trap ทุกตัวใช้ WorldTime.Dt(rawDt) แทน rawDt โดยตรง
/// Player ไม่ได้รับผล (ใช้ rawDt ตามปกติ)
/// </summary>
public static class WorldTime
{
    public static float Scale     { get; private set; } = 1f;
    public static bool  IsFrozen  { get; private set; } = false;

    private const float SlowScale   = 0.3f;
    private const float NormalScale = 1f;

    public static void SetSlow()   => Scale = SlowScale;
    public static void SetNormal() => Scale = NormalScale;

    /// <summary>หยุดทุกอย่างในเกม (goal/game over)</summary>
    public static void Freeze()    => IsFrozen = true;

    /// <summary>รีเซ็ตสำหรับการเริ่มด่านใหม่</summary>
    public static void Reset()     { Scale = NormalScale; IsFrozen = false; }

    /// <summary>คืน dt ที่ปรับ scale แล้ว — 0 เมื่อ frozen</summary>
    public static float Dt(float rawDt) => IsFrozen ? 0f : rawDt * Scale;
}
