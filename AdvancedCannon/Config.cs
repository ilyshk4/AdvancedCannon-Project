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
        [XmlElement] public CollisionConfig Collision = new CollisionConfig();
        [XmlElement] public ShellConfig Shell = new ShellConfig();
        [XmlElement] public TraceConfig Trace = new TraceConfig();
        [XmlElement] public PenetrationConfig Penetration = new PenetrationConfig();
        [XmlElement] public RicochetConfig Ricochet = new RicochetConfig();
        [XmlElement] public ArmorPiercingCapConfig ArmorPiercingCap = new ArmorPiercingCapConfig();
        [XmlElement] public BallisticCapConfig BallisticCap = new BallisticCapConfig();
        [XmlElement] public SpallingConfig Spalling = new SpallingConfig();
        [XmlElement] public SurfaceConfig Surface = new SurfaceConfig();
        [XmlElement] public ShellsConfig Shells = new ShellsConfig();

        // [XmlElement] public float CollisionDamageScale = 0.01F;
        // [XmlElement] public float CollisionForceScale = 3F;
        // [XmlElement] public float MinProjectileVelocity = 100F;
        // [XmlElement] public float BaseProjectileDrag = 0.20F;
        // [XmlElement] public float PenetrationMassLoose = 0.1F;
        // [XmlElement] public float ShellScale = 0.333F;
        // 
        // [XmlElement] public float BaseExitAngle = 10;
        // 
        // [XmlElement] public float ArmorPiercingCapAngleReduce = 10F;
        // 
        // [XmlElement] public float BallisticCapDrag = 0.10F;
        // 
        // [XmlElement] public float BaseFragmentsConeAngle = 45;
        // [XmlElement] public float BaseExplosionConeAngle = 180;
        // 
        // [XmlElement] public float RicochetVelocityDecreasePower = 16;
        // 
        // [XmlElement] public float ShellTimeToLive = 2F;
        // [XmlElement] public float TrailTimeToLive = 1F;
        // 
        // [XmlElement] public float SpallingFragmentTimeToLive = 0.05F;
        // [XmlElement] public float SpallingFragmentsFactor = 0.75F;
        // [XmlElement] public float SpallingConeFactor = 1F;
        // 
        // [XmlElement] public int APHE_ParticlesCountPerKilo = 25;
        // [XmlElement] public float APHE_ParticleTimeToLive = 0.05F;
        // 
        // [XmlElement] public int AP_APHE_VelocityDivisor = 2080;
        // 
        // [XmlElement] public int HE_MinFragmentsCount = 5;
        // [XmlElement] public int HE_FragmentsCountPerKilo = 10;
        // [XmlElement] public float HE_FragmentTimeToLive = 0.03F;
        // [XmlElement] public float HE_FragmentMass = 0.05F;
        // [XmlElement] public float HE_BaseVelocity = 150F;
        // [XmlElement] public float HE_VelocityPerKilo = 1500F;
        // [XmlElement] public float HE_FragmentCaliber = 10F;
        // 
        // [XmlElement] public float HESH_PenetrationPerKilo = 100;
        // [XmlElement] public float HESH_BaseSpallingCount = 20;
        // [XmlElement] public float HESH_BaseConeAngle = 90;
        // 
        // [XmlElement] public float FSDS_CaliberScale = 0.25F;
        // [XmlElement] public float FSDS_SpallingCaliberScale = 0.1F;
        // [XmlElement] public float FSDS_ConeScale = 3F;
        // [XmlElement] public float FSDS_AngleReduce = 30F;
        // 
        // [XmlElement] public float HEAT_FragmentMass = 0.01F;
        // [XmlElement] public float HEAT_VelocityPerKilo = 1500F;
        // 
        // [XmlElement] public float Surface_BaseDensity = 0.15F;
    }

    public class CollisionConfig
    {
        [XmlElement] public float DamageScale = 0.01F;
        [XmlElement] public float ForceScale = 3F;
    }

    public class ShellConfig
    {
        [XmlElement] public float MinVelocity = 100F;
        [XmlElement] public float BaseDrag = 0.20F;
        [XmlElement] public float Scale = 0.333F;
        [XmlElement] public float TimeToLive = 2F;
    }

    public class TraceConfig
    {
        [XmlElement] public float TimeToLive = 1F;
    }

    public class PenetrationConfig
    {
        [XmlElement] public float BaseExitAngle = 10;
        [XmlElement] public float MassLoose = 0.1F;
    }

    public class RicochetConfig
    {
        [XmlElement] public float VelocityDecreasePower = 16;
    }

    public class ArmorPiercingCapConfig
    {
        [XmlElement] public float AngleReduce = 10F;
    }
    public class BallisticCapConfig
    {
        [XmlElement] public float Drag = 0.1F;
    }

    public class SpallingConfig
    {
        [XmlElement] public float BaseConeAngle = 45;
        [XmlElement] public float CountFactor = 0.75F;
        [XmlElement] public float ConeFactor = 1F;
        [XmlElement] public float TimeToLive = 0.05F;
    }

    public class ShellsConfig
    {
        [XmlElement] public APConfig AP = new APConfig();
        [XmlElement] public APHEConfig APHE = new APHEConfig();
        [XmlElement] public HEConfig HE = new HEConfig();
        [XmlElement] public APFSDSConfig APFSDS = new APFSDSConfig();
        [XmlElement] public HESHConfig HESH = new HESHConfig();
        [XmlElement] public HEATConfig HEAT = new HEATConfig();
    }
    public class APConfig
    {
        [XmlElement] public float ArmorResistanceFactor = 2080;
    }

    public class APHEConfig
    {
        [XmlElement] public int ParticlesCountPerKilo = 25;
        [XmlElement] public float ParticleTimeToLive = 0.05F;
    }

    public class HEConfig
    {
        [XmlElement] public int MinFragmentsCount = 5;
        [XmlElement] public int FragmentsCountPerKilo = 10;
        [XmlElement] public float FragmentTimeToLive = 0.03F;
        [XmlElement] public float FragmentMass = 0.05F;
        [XmlElement] public float BaseVelocity = 150F;
        [XmlElement] public float VelocityPerKilo = 1500F;
        [XmlElement] public float FragmentCaliber = 10F;

    }

    public class APFSDSConfig
    {
        [XmlElement] public float CaliberScale = 0.25F;
        [XmlElement] public float ConeScale = 3F;
        [XmlElement] public float AngleReduce = 30F;

    }

    public class HESHConfig
    {
        [XmlElement] public float PenetrationPerKilo = 100;
        [XmlElement] public float BaseSpallingCount = 20;
        [XmlElement] public float BaseConeAngle = 90;
    }

    public class HEATConfig
    {
        [XmlElement] public float FragmentMass = 0.01F;
        [XmlElement] public float VelocityPerKilo = 4000F;
    }

    public class SurfaceConfig
    {
        [XmlElement] public float BaseDensity = 0.15F;
    }
}
