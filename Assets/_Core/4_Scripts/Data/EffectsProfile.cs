using System.Collections.Generic;
using UnityEngine;
using VTLTools;
using VTLTools.Effect;

[CreateAssetMenu(fileName = "EffectsProfile", menuName = "ScriptableObjects/EffectsProfile")]
public class EffectsProfile : ScriptableObject
{
    #region Fields
    private const string ITEM_RESOURCE_FOLDER_PATH = "Data/EffectsProfile";

    private static ResourceAsset<EffectsProfile> asset = new(ITEM_RESOURCE_FOLDER_PATH);

    public List<EffectConfig> effects = new List<EffectConfig>();
    #endregion

    #region Properties

    #endregion

    #region Lifecycle

    #endregion

    #region Private Methods

    #endregion

    #region Public Methods
    public static Effect GetData(EffectId _id)
    {
        EffectsProfile profile = asset.Value;
        return profile != null ? profile.effects.Find(e => e.id == _id)?.effect : null;
    }

    public static void Prewarm(EffectId id, int count)
    {
        Effect prefab = GetData(id);
        if (prefab != null && count > 0)
        {
            ObjectPool.CreatePool(prefab, count);
        }
    }

    public static Effect SpawnEffect(EffectId _id, Vector3 position, Transform parent = null)
    {
        return SpawnEffect(_id, position, null, null, parent);
    }

    public static Effect SpawnEffect(EffectId _id, Vector3 position, Vector3 localEulerAngles, Transform parent = null)
    {
        return SpawnEffect(_id, position, (Vector3?)localEulerAngles, null, parent);
    }

    public static Effect SpawnEffect(EffectId _id, Vector3 position, Vector3 localEulerAngles, Vector3 shapeBoxSize, Transform parent = null)
    {
        return SpawnEffect(_id, position, localEulerAngles, (Vector3?)shapeBoxSize, parent);
    }

    private static Effect SpawnEffect(EffectId _id, Vector3 position, Vector3? localEulerAngles, Vector3? shapeBoxSize, Transform parent)
    {
        Effect prefab = GetData(_id);
        if (prefab == null)
        {
            return null;
        }

        Effect fx = ObjectPool.Spawn(prefab);
        fx.Init(position, parent);
        ApplyLocalEulerAngles(fx, localEulerAngles);
        ApplyShapeBox(fx, shapeBoxSize);
        if (shapeBoxSize.HasValue)
        {
            fx.RefreshBurstCountFromShapeArea();
        }
        fx.Play();
        return fx;
    }

    public static Effect SpawnEffect(EffectId _id, Vector3 position, Color color, Transform parent = null)
    {
        return SpawnEffect(_id, position, color, null, parent);
    }

    public static Effect SpawnEffect(EffectId _id, Vector3 position, Color color, Vector3 localEulerAngles, Transform parent = null)
    {
        return SpawnEffect(_id, position, color, (Vector3?)localEulerAngles, parent);
    }

    private static Effect SpawnEffect(EffectId _id, Vector3 position, Color color, Vector3? localEulerAngles, Transform parent)
    {
        Effect prefab = GetData(_id);
        if (prefab == null)
        {
            return null;
        }

        Effect fx = ObjectPool.Spawn(prefab);
        fx.Init(position, parent);
        ApplyLocalEulerAngles(fx, localEulerAngles);
        fx.SetParticleColor(color);
        fx.Play();
        return fx;
    }

    public static Effect SpawnEffect(EffectId _id, Vector3 position, Color color, int count, Transform parent = null)
    {
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = position,
            startColor = color,
            applyShapeToPosition = true
        };

        return SpawnEffect(_id, emitParams, count, null, parent);
    }

    public static Effect SpawnEffect(EffectId _id, Vector3 position, Color color, int count, Vector3 localEulerAngles, Transform parent = null)
    {
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = position,
            startColor = color,
            applyShapeToPosition = true
        };

        return SpawnEffect(_id, emitParams, count, localEulerAngles, parent);
    }

    public static Effect SpawnEffect(EffectId _id, ParticleSystem.EmitParams emitParams, int count, Transform parent = null)
    {
        return SpawnEffect(_id, emitParams, count, null, parent);
    }

    public static Effect SpawnEffect(EffectId _id, ParticleSystem.EmitParams emitParams, int count, Vector3 localEulerAngles, Transform parent = null)
    {
        return SpawnEffect(_id, emitParams, count, (Vector3?)localEulerAngles, parent);
    }

    private static Effect SpawnEffect(EffectId _id, ParticleSystem.EmitParams emitParams, int count, Vector3? localEulerAngles, Transform parent)
    {
        Effect prefab = GetData(_id);
        if (prefab == null)
        {
            return null;
        }

        Effect fx = ObjectPool.Spawn(prefab);
        fx.Init(Vector3.zero, parent);
        ApplyLocalEulerAngles(fx, localEulerAngles);
        fx.SetParticleColor(emitParams.startColor);
        fx.Play();

        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps != null && count > 0)
        {
            ps.Emit(emitParams, count);
        }

        return fx;
    }

    public static Effect SpawnEffect(EffectId _id, IList<Vector3> shapeWorldPoints, Color color, Transform parent = null)
    {
        return SpawnEffect(_id, shapeWorldPoints, color, new Vector3(-90f, 0f, 0f), parent);
    }

    public static Effect SpawnEffect(EffectId _id, IList<Vector3> shapeWorldPoints, Color color, Vector3 localEulerAngles, Transform parent = null)
    {
        Effect prefab = GetData(_id);
        if (prefab == null)
        {
            return null;
        }

        Effect fx = ObjectPool.Spawn(prefab);
        fx.Init(Vector3.zero, parent);
        fx.SetParticleShapeBoxFromWorldPoints(shapeWorldPoints);
        fx.SetParticleColor(color);
        ApplyLocalEulerAngles(fx, localEulerAngles);
        ApplyBurstCountFromShapeArea(fx);
        fx.Play();
        return fx;
    }

    public static Effect SpawnEffect(EffectId _id, Vector3 position, int colorId, Transform parent = null)
    {
        return SpawnEffect(_id, position, parent);
    }

    #endregion

    private static void ApplyLocalEulerAngles(Effect fx, Vector3? localEulerAngles)
    {
        if (fx == null || !localEulerAngles.HasValue)
        {
            return;
        }

        fx.transform.localEulerAngles = localEulerAngles.Value;
    }

    private static void ApplyShapeBox(Effect fx, Vector3? shapeBoxSize)
    {
        if (fx == null || !shapeBoxSize.HasValue)
        {
            return;
        }

        fx.SetParticleShapeBox(shapeBoxSize.Value);
    }

    private static void ApplyBurstCountFromShapeArea(Effect fx)
    {
        fx?.RefreshBurstCountFromShapeArea();
    }

    public enum EffectId
    {
        Starts_Sparks,
        Sticker_Snap
    }
    [System.Serializable]
    public class EffectConfig
    {
        public EffectId id;
        public Effect effect;
    }
}
