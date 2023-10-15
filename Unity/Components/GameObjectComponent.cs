using System;
using Scellecs.Morpeh;
using UnityEngine;

namespace Scellecs.Morpeh.Components
{
    [Serializable]
    public struct GameObjectComponent : IComponent
    {
        public GameObject gameObject;
    }
}