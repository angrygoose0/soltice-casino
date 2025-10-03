using System.Collections.Generic;
using UnityEngine;

namespace BalloonFX
{

[RequireComponent(typeof(ParticleSystem))]
public class ParticleDeathSound : MonoBehaviour
{
    public AudioClip deathSound;  // Sound effect to play on particle death
    public float volume = 1.0f;   // Volume of the sound
    public float pitchVariance = 0.2f;  // Pitch variance (20% by default)
    [Range(0f, 1f)]
    public float playChance = 1.0f;  // Chance to play sound, default 100%

    private ParticleSystem ps;  // Renamed variable
    private List<ParticleSystem.Particle> aliveParticles = new List<ParticleSystem.Particle>();
    private AudioSource audioSource;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();  // Updated to use ps
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void LateUpdate()
    {
        int maxParticles = ps.main.maxParticles;  // Updated to use ps
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[maxParticles];

        // Get currently alive particles
        int aliveCount = ps.GetParticles(particles);  // Updated to use ps

        // Check for dead particles by comparing with the previously alive list
        for (int i = 0; i < aliveParticles.Count; i++)
        {
            bool particleStillAlive = false;

            for (int j = 0; j < aliveCount; j++)
            {
                if (aliveParticles[i].randomSeed == particles[j].randomSeed)
                {
                    particleStillAlive = true;
                    break;
                }
            }

            // If the particle is no longer alive, it has "died"
            if (!particleStillAlive)
            {
                if (Random.value <= playChance)
                {
                    PlayDeathSound();
                }
            }
        }

        // Update alive particles list
        aliveParticles.Clear();
        for (int i = 0; i < aliveCount; i++)
        {
            aliveParticles.Add(particles[i]);
        }
    }

    void PlayDeathSound()
    {
        if (deathSound != null)
        {
            // Apply pitch variance
            audioSource.pitch = 1.0f + Random.Range(-pitchVariance, pitchVariance);
            audioSource.PlayOneShot(deathSound, volume);
        }
    }
}
}