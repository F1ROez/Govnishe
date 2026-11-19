using System;
using UnityEngine;
namespace Brainrot
{
    [Serializable]public sealed class BoomerangTuning
    {
        [Range(2,14),Tooltip("Дальность броска биты, м.")]public float Distance=7;
        [Range(.15f,1),Tooltip("Время полёта от героя, с.")]public float OutSeconds=.32f;
        [Range(.2f,1.5f),Tooltip("Время возвращения к движущемуся герою, с.")]public float ReturnSeconds=.42f;
        [Range(.1f,.8f),Tooltip("Радиус попадания летящей биты, м.")]public float HitRadius=.35f;
        [Range(.1f,2),Tooltip("Множитель обычного урона на каждом проходе.")]public float DamageMultiplier=1;
    }
}
