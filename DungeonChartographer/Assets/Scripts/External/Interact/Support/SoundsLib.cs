using UnityEngine;

[CreateAssetMenu]
public class SoundsLib : ScriptableObject
{
    [SerializeField] SoundPlay[] sounds;
    public SoundPlay[] Sounds => sounds;
}

