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
        public const int ModVersion = 93;
        [XmlElement] public int Version = ModVersion;
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
    }
     
    public class CollisionConfig
    { 
        [XmlElement] public float DamageScale = 0.01F;
        [XmlElement] public float ForceScale = 2F;
    }

    public class ShellConfig
    {
        [XmlElement] public float MinShellVelocity = 10F;
        [XmlElement] public float MinFragmentVelocity = 15F;
        [XmlElement] public float BaseDrag = 0.20F;
        [XmlElement] public float Scale = 0.2F;
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
        [XmlElement] public float ForceCountFactor = 0.75F;
        [XmlElement] public float ThicknessFactor = 0.01F;
        [XmlElement] public float ForceConeFactor = 1F;
        [XmlElement] public float TimeToLive = 0.2F;
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
        [XmlElement] public float Power = 1F;
        [XmlElement] public float ArmorResistanceFactor = 2080;
    }

    public class APHEConfig
    {
        [XmlElement] public int MinParticlesCount = 5;
        [XmlElement] public int ParticlesCountPerKilo = 25;
        [XmlElement] public float ParticleTimeToLive = 0.03F;
    }

    public class HEConfig
    {
        [XmlElement] public float FragmentTimeToLive = 0.1F;
        [XmlElement] public float Velocity = 2000F;
    }

    public class APFSDSConfig
    {
        [XmlElement] public float CaliberScale = 0.25F;
        [XmlElement] public float AngleReduce = 30F;

    }

    public class HESHConfig
    {
        [XmlElement] public float PenetrationValue = 65F;
        [XmlElement] public float PenetrationPower = 0.4F;
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