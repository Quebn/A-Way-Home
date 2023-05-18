using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance {get; private set;}
    [SerializeField] private Audio[] sfxs;

    private void Awake()
    {
        if(instance == null)
            instance = this;
        for(int i = 0; i < sfxs.Length; i++)
            sfxs[i].InitSource(gameObject.AddComponent<AudioSource>());
    }
}
