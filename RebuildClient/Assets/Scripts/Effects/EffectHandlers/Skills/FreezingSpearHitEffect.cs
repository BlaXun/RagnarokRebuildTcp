using Assets.Scripts.Effects.PrimitiveData;
using Assets.Scripts.Network;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectHandlers.Skills
{
    [RoEffect("FreezingSpearHit")]
    public class FreezingSpearHitEffect : IEffectHandler
    {
        public static void LaunchFreezingSpearEffect(ServerControllable target, int level) {
            AudioManager.Instance.OneShotSoundEffect(target.Id, $"ef_frostdiver2.ogg", target.transform.position);

            // How many milliseconds the effect should be visible
            var duration = (60 * 3) + (level * 20);
            var effect = RagnarokEffectPool.Get3dEffect(EffectType.FreezingSpearHit);
            effect.SetDurationByFrames(duration);
            effect.SourceEntity = target;
            effect.FollowTarget = null;
            effect.DestroyOnTargetLost = false;
            effect.UpdateOnlyOnFrameChange = true;
            effect.transform.position = target.transform.position.SnapToWorldHeight();
            effect.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), Random.Range(0, 10));
            effect.transform.localScale = new Vector3(1f, 1f, 1f);
            effect.Material = EffectSharedMaterialManager.GetMaterial(EffectMaterialType.IceMaterial);

            {
                //spawn a new spike
                var prim = effect.LaunchPrimitive(PrimitiveType.Spike3D, effect.Material,  (float) duration/ 100.0f);
                var data = prim.GetPrimitiveData<Spike3DData>();

                prim.transform.localPosition = new Vector3(0, -2f, 0);
                prim.transform.localScale = Vector3.one;
                prim.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), Random.Range(0, 10));

                data.Height = 18f / 5f;
                data.Size = Random.Range(3f, 3.5f) / 5f;
                data.Alpha = 255;
                data.AlphaSpeed = 0;
                data.AlphaMax = 255;
                data.Speed = 1f / 5f;
                data.Acceleration = 0.01f;
                data.Flags = Spike3DFlags.SpeedLimit | Spike3DFlags.ReturnDown;
                data.StopStep = 15; // At what step should the spike stop rising from the ground?
                data.ChangeStep = 12;
                data.ChangeSpeed = -1.2f / 5f;
                data.ChangeAccel = 0;
                data.FadeOutLength = 0.2f; // When should the fade out begin? The higher the number the earlier it will begin (For example 0.3f makes fade out start when the lifetime has reached 70%)
                
            }

            //secondary spikes
            for (var i = 0; i < 6; i++)
            {
                //spawn a new spike
                var prim = effect.LaunchPrimitive(PrimitiveType.Spike3D, effect.Material, duration / 100.0f);
                var data = prim.GetPrimitiveData<Spike3DData>();
            
                var dist = 3.5f / 5f;
                var angle = (Random.Range(0, 60f) + 60 * i) * Mathf.Deg2Rad;
                prim.transform.localPosition = new Vector3(Mathf.Sin(angle) * dist, -4f, Mathf.Cos(angle) * dist);
                prim.transform.localScale = Vector3.one;
                prim.transform.localRotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 10);
            
                data.Height = 20f / 5f;
                data.Size = Random.Range(4f, 5f) / 5f;
                data.Alpha = 255;
                data.AlphaSpeed = 0;
                data.AlphaMax = 255;
                data.Speed = 1.5f / 5f;
                data.Acceleration = 0f;
                data.Flags = Spike3DFlags.SpeedLimit;
                data.StopStep = 3;
                data.FadeOutLength = 0.2f;
            }
        }
    }
}