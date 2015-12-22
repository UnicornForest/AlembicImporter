﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace AlembicExporterTest
{

    public class ParticleEngine : MonoBehaviour
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct peParticle
        {
            public const int size = 32;

            [FieldOffset(0)]
            public Vector3 position;
            [FieldOffset(0)]
            public Vector4 position4;
            [FieldOffset(16)]
            public Vector3 velocity;
            [FieldOffset(16)]
            public Vector4 velocity4;
        }


        public struct CSParams
        {
            public const int size = 32;

            public int particle_count;
            public float particle_size;
            public float rcp_particle_size2;
            public float pressure_stiffness;
            public float wall_stiffness;
            public float timestep;

            float pad1, pad2;
        };


        const int KernelBlockSize = 256;

        public int m_particle_count = 1024 * 4;
        public float m_particle_size = 0.1f;
        public float m_pressure_stiffness = 500.0f;
        public float m_wall_stiffness = 1500.0f;

        public ComputeShader m_cs_particle_core;
        ComputeBuffer m_cb_params;
        ComputeBuffer m_cb_particles;
        ComputeBuffer m_cb_positions;
        CSParams[] m_csparams;
        Vector3[] m_buf_positions;


        public Vector3[] GetPositionBuffer()
        {
            return m_buf_positions;
        }



        void InitializeSimulation()
        {
            if (SystemInfo.supportsComputeShaders)
            {
                m_cb_params = new ComputeBuffer(1, CSParams.size);
                m_cb_particles = new ComputeBuffer(m_particle_count, peParticle.size);
                m_cb_positions = new ComputeBuffer(m_particle_count, 12);
                m_buf_positions = new Vector3[m_particle_count];
                m_csparams = new CSParams[1];
                for (int i = 0; i < 2; ++i)
                {
                    m_cs_particle_core.SetBuffer(i, "g_params", m_cb_params);
                    m_cs_particle_core.SetBuffer(i, "g_particles", m_cb_particles);
                    m_cs_particle_core.SetBuffer(i, "g_positions", m_cb_positions);
                }
            }
            {
                UnityEngine.Random.seed = 0;
                var tmp = new peParticle[m_particle_count];
                for (int i = 0; i < tmp.Length; ++i)
                {
                    tmp[i].position = new Vector3(
                        UnityEngine.Random.Range(-5.0f, 5.0f),
                        UnityEngine.Random.Range(-5.0f, 5.0f) + 5.0f,
                        UnityEngine.Random.Range(-5.0f, 5.0f));
                }
                m_cb_particles.SetData(tmp);
            }
        }

        void ReleaseSimulation()
        {
            if (m_cb_params != null)
            {
                m_cb_params.Release(); m_cb_params = null;
                m_cb_particles.Release(); m_cb_particles = null;
                m_cb_positions.Release(); m_cb_positions = null;
            }
        }

        void UpdateSimulation(float dt)
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.Log("ComputeShader is not available.");
                return;
            }

            m_csparams[0].particle_count = m_particle_count;
            m_csparams[0].particle_size = m_particle_size;
            m_csparams[0].rcp_particle_size2 = 1.0f / (m_particle_size * 2.0f);
            m_csparams[0].pressure_stiffness = m_pressure_stiffness;
            m_csparams[0].wall_stiffness = m_wall_stiffness;
            m_csparams[0].timestep = dt;
            m_cb_params.SetData(m_csparams);
            for (int i = 0; i < 2; ++i)
            {
                m_cs_particle_core.SetBuffer(i, "g_params", m_cb_params);
                m_cs_particle_core.SetBuffer(i, "g_particles", m_cb_particles);
                m_cs_particle_core.SetBuffer(i, "g_positions", m_cb_positions);
            }

            m_cs_particle_core.Dispatch(0, m_particle_count / KernelBlockSize, 1, 1);
            m_cs_particle_core.Dispatch(1, m_particle_count / KernelBlockSize, 1, 1);

            m_cb_positions.GetData(m_buf_positions);
        }



        void OnEnable()
        {
            InitializeSimulation();
        }

        void OnDisable()
        {
            ReleaseSimulation();
        }

        void Update()
        {
            UpdateSimulation(Time.deltaTime);
        }

    }
}