using UnityEngine;
using MoreMountains.Feedbacks;
using System.Collections.Generic;

public class SpringManager : MonoBehaviour
{
    // Pool of springs by key
    private Dictionary<string, MMSpringFloat> springs = new Dictionary<string, MMSpringFloat>();
    
    // All springs for batch updates
    private List<MMSpringFloat> activeSprings = new List<MMSpringFloat>();

    void Update()
    {
        float deltaTime = Time.deltaTime;
        for (int i = activeSprings.Count - 1; i >= 0; i--)
        {
            activeSprings[i].UpdateSpringValue(deltaTime);
        }
    }

    /// <summary>
    /// Get or create a spring by key
    /// </summary>
    public MMSpringFloat GetSpring(string key, float damping = 0.4f, float frequency = 6f, float initialValue = 0f)
    {
        if (springs.TryGetValue(key, out MMSpringFloat spring))
        {
            return spring;
        }
        
        spring = new MMSpringFloat();
        spring.Damping = damping;
        spring.Frequency = frequency;
        spring.MoveToInstant(initialValue);
        
        springs[key] = spring;
        activeSprings.Add(spring);
        
        return spring;
    }

    /// <summary>
    /// Remove a spring when no longer needed
    /// </summary>
    public void RemoveSpring(string key)
    {
        if (springs.TryGetValue(key, out MMSpringFloat spring))
        {
            activeSprings.Remove(spring);
            springs.Remove(key);
        }
    }

    /// <summary>
    /// Trigger a spring action
    /// </summary>
    public void Bump(string key, float amount = 1f)
    {
        if (springs.TryGetValue(key, out MMSpringFloat spring))
        {
            spring.Bump(amount);
        }
    }

    public void MoveTo(string key, float value)
    {
        if (springs.TryGetValue(key, out MMSpringFloat spring))
        {
            spring.MoveTo(value);
        }
    }

    public float GetValue(string key)
    {
        if (springs.TryGetValue(key, out MMSpringFloat spring))
        {
            return spring.CurrentValue;
        }
        return 0f;
    }
}