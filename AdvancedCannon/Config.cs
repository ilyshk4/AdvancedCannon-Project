using Modding.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace AdvancedCannon
{
    public class Config : Element
    {
        [XmlElement] public float CollisionDamageScale = 0.001F;
        [XmlElement] public float CollisionForceScale = 10F;
        [XmlElement] public float MinProjectileVelocity = 100F;
        [XmlElement] public float BaseProjectileDrag = 0.20F;
        [XmlElement] public float PenetrationMassLoose = 0.1F;

        [XmlElement] public float BaseExitAngle = 10;

        [XmlElement] public float ArmorPiercingCapDrag = 0.15F;
        [XmlElement] public float ArmorPiercingCapAngleReduce = 15F;

        [XmlElement] public float BallisticCapDrag = 0.10F;
        [XmlElement] public float BallisticCapExitAngle = 5;

        [XmlElement] public float BaseFragmentsConeAngle = 90;
        [XmlElement] public float BaseExplosionConeAngle = 180;

        [XmlElement] public float RicochetVelocityDecreasePower = 2;

        [XmlElement] public float ShellTimeToLive = 5F;

        [XmlElement] public float SpallingFragmentTimeToLive = 0.05F;
        [XmlElement] public int SpallingFragmentsCount = 5;

        [XmlElement] public int ExplosiveParticlesCountPerKilo = 25;
        [XmlElement] public float ExplosiveParticleTimeToLive = 0.05F;

        [XmlElement] public int HighExplosiveFragmentsCountPerKilo = 10;
        [XmlElement] public float HighExplosiveFragmentBaseVelocity = 150F;
        [XmlElement] public float HighExplosiveFragmentVelocityPerKilo = 2500F;
        [XmlElement] public float HighExplosiveFragmentTimeToLive = 0.03F;

        [XmlElement] public float FSDS_SpallingCaliberScale = 0.1F;
        [XmlElement] public float FSDS_ConeScale = 3F;
    }
}
