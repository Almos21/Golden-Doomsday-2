// CharacterJetpackManaged.cs — reemplaza CharacterJetpack en el inspector
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    public class CharacterJetpackManaged : CharacterJetpack
    {
        // CharacterTransformation setea esto en true cuando quiere control exclusivo
        [HideInInspector]
        public bool ExternalFuelControl = false;

        protected override void Refuel()
        {
            if (ExternalFuelControl) return; // cede control total
            base.Refuel();
        }
    }
}