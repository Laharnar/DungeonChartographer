using UnityEngine;

namespace Combat
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "Faction", menuName = "ScriptableObjects/Faction", order = 1)]
    public class Faction : ScriptableObject
    {
        [SerializeField] private string alias;
        [SerializeField] private Color color = Color.white;

        static Faction playerFaction;

        public bool IsPlayer()
        {
            return playerFaction == this;
        }

        public string Alias { get { return alias; } }
        public Color Color { get { return color; } }
    }
    [System.Serializable]
    public enum FactionSide
    {
        Neutral,
        Ally,
        Enemy,
        Player,
    }
}
