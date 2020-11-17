//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

#if !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2
#define UNITY_53_OR_GREATER
#endif

using UnityEngine;

namespace EVP
{

[RequireComponent(typeof(ParticleSystem))]
public class TireParticleEmitter : MonoBehaviour
	{
	public enum Mode { PressureAndSkid, PressureAndVelocity }

	public Mode mode = Mode.PressureAndSkid;

	public float emissionRate = 10.0f;
	[Range(0,1)]
	public float emissionShuffle = 0.0f;
	public float maxLifetime = 7.0f;
	public float minVelocity = 1.0f;
	public float maxVelocity = 15.0f;
	[Range(0,1)]
	public float tireVelocityRatio = 0.5f;

	public Color Color1 = Color.white;
	public Color Color2 = Color.gray;
	public bool randomColor = false;


	ParticleSystem m_particles;

	#if UNITY_53_OR_GREATER
	ParticleSystem.EmitParams m_emitParams = new ParticleSystem.EmitParams();
	#else
	ParticleSystem.Particle m_particle;
	#endif


	void OnEnable ()
		{
		m_particles = GetComponent<ParticleSystem>();
		m_particles.Stop();
		}


	public float EmitParticle (Vector3 position, Vector3 wheelVelocity, Vector3 tireVelocity, float pressureRatio, float intensityRatio, float lastParticleTime)
		{
		if (!isActiveAndEnabled) return -1.0f;

		// Ensure first particle is emitted on new sequence started
		if (lastParticleTime < 0.0f) lastParticleTime = Time.time - 1.0f/emissionRate;

		int particleCount = (int)((Time.time - lastParticleTime) * emissionRate);
		if (particleCount <= 0)
			return lastParticleTime;

		// Base lifetime of the particles depend on the mode

		float baseLifetime = 0.0f;

		switch (mode)
			{
			case Mode.PressureAndSkid:
				baseLifetime = pressureRatio * intensityRatio * maxLifetime;
				break;

			case Mode.PressureAndVelocity:
				float velocity = tireVelocity.magnitude + wheelVelocity.magnitude;
				baseLifetime = pressureRatio * maxLifetime * Mathf.InverseLerp(minVelocity, maxVelocity, velocity);
				break;
			}

		if (baseLifetime <= 0.0f)
			return -1.0f;

		for (int i = 0; i < particleCount; i++)
			{
			// The actual tire velocity (aka forward skip in 3D world) affects the
			// initial velocity of the particles

			Vector3 velocity = wheelVelocity * 0.9f + tireVelocity * tireVelocityRatio;

			float lifetime = baseLifetime * Random.Range(0.6f, 1.4f);

			float size = lifetime / maxLifetime * Random.Range(0.8f, 1.4f);
			float rotation = Random.Range(0.0f, 360.0f);

			Color color = randomColor? Color.Lerp(Color1, Color2, Random.value) : Color1;
			uint randomSeed = (uint)Random.Range(0, 20000);

			#if UNITY_53_OR_GREATER
			m_emitParams.position = position;
			m_emitParams.rotation = rotation;

			m_emitParams.velocity = velocity;
			m_emitParams.angularVelocity = 0.0f;

			m_emitParams.startLifetime = lifetime;
			m_emitParams.startSize = size;
			m_emitParams.startColor = color;

			m_emitParams.randomSeed = randomSeed;

			m_particles.Emit(m_emitParams, 1);
			#else
			m_particle.position = position;
			m_particle.velocity = velocity;

			m_particle.lifetime = lifetime;
			m_particle.startLifetime = lifetime;

			m_particle.size = size;
			m_particle.rotation = rotation;
			m_particle.angularVelocity = 0.0f;

			m_particle.color = color;
			m_particle.randomSeed = randomSeed;

			m_particles.Emit(m_particle);
			#endif
			}

		return Time.time + Random.Range(-emissionShuffle, +emissionShuffle) / emissionRate;
		}
	}
}