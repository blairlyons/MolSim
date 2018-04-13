﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AICS.AgentSim
{
    public class Reactor : MonoBehaviour 
    {
        [HideInInspector] public Container container;
        [HideInInspector] public ComplexSpawner spawner;
        public Model model;
        public List<BimolecularReactionSimulator> bimolecularReactionSimulators = new List<BimolecularReactionSimulator>();
        public List<CollisionFreeReactionSimulator> collisionFreeReactionSimulators = new List<CollisionFreeReactionSimulator>();
        [Tooltip( "How many attempts to move particles each frame? collisions and boundaries can cause move to fail" )]
        public int maxMoveAttempts = 20;
        [Tooltip( "Reflect particle to other side of container when it runs into a wall?" )]
        public bool periodicBoundary = true;

        public List<ParticleSimulator> particleSimulators = new List<ParticleSimulator>();
        public List<ComplexSimulator> complexSimulators = new List<ComplexSimulator>();
        protected List<ParticleSimulator> particleSimulatorsToDestroy = new List<ParticleSimulator>();
        protected List<ComplexSimulator> complexSimulatorsToDestroy = new List<ComplexSimulator>();

        float dT
        {
            get
            {
                return World.Instance.dT;
            }
        }

        void Start ()
        {
            CreateReactionSimulators();
            CreateContainer();
            SpawnComplexes();
        }

        protected virtual void CreateReactionSimulators ()
        {
            model.Init(); //for prototyping in inspector without writing custom property drawer etc

            foreach (Reaction reaction in model.reactions)
            {
                if (reaction.isBimolecular)
                {
                    bimolecularReactionSimulators.Add( new BimolecularReactionSimulator( reaction ) );
                }
                else
                {
                    collisionFreeReactionSimulators.Add( new CollisionFreeReactionSimulator( reaction ) );
                }
            }
        }

        protected virtual void CreateContainer ()
        {
            container = gameObject.AddComponent<Container>();
            container.Init( model.scale, model.containerVolume, periodicBoundary );
        }

        protected virtual void SpawnComplexes ()
        {
            spawner = gameObject.AddComponent<ComplexSpawner>();
            spawner.Init( this );
            foreach (ComplexConcentration complex in model.complexes)
            {
                spawner.SpawnComplexes( complex );
            }
        }

        public BimolecularReactionSimulator[] GetRelevantBimolecularReactionSimulators (ComplexState complexState)
        {
            List<BimolecularReactionSimulator> reactionSimulatorsList = new List<BimolecularReactionSimulator>();
            foreach (BimolecularReactionSimulator reactionSimulator in bimolecularReactionSimulators)
            {
                if (reactionSimulator.IsReactant( complexState ))
                {
                    reactionSimulatorsList.Add( reactionSimulator );
                }
            }
            return reactionSimulatorsList.ToArray();
        }

        public BimolecularReactionSimulator[] GetRelevantBimolecularReactionSimulators (MoleculeSimulator[] complex)
        {
            List<BimolecularReactionSimulator> reactionSimulatorsList = new List<BimolecularReactionSimulator>();
            foreach (BimolecularReactionSimulator reactionSimulator in bimolecularReactionSimulators)
            {
                if (reactionSimulator.IsReactant( complex ))
                {
                    reactionSimulatorsList.Add( reactionSimulator );
                }
            }
            return reactionSimulatorsList.ToArray();
        }

        public CollisionFreeReactionSimulator[] GetRelevantCollisionFreeReactionSimulators (ComplexState complexState)
        {
            List<CollisionFreeReactionSimulator> reactionSimulatorsList = new List<CollisionFreeReactionSimulator>();
            foreach (CollisionFreeReactionSimulator reactionSimulator in collisionFreeReactionSimulators)
            {
                if (reactionSimulator.IsReactant( complexState ))
                {
                    reactionSimulatorsList.Add( reactionSimulator );
                }
            }
            return reactionSimulatorsList.ToArray();
        }

        public CollisionFreeReactionSimulator[] GetRelevantCollisionFreeReactionSimulators (MoleculeSimulator[] complex)
        {
            List<CollisionFreeReactionSimulator> reactionSimulatorsList = new List<CollisionFreeReactionSimulator>();
            foreach (CollisionFreeReactionSimulator reactionSimulator in collisionFreeReactionSimulators)
            {
                if (reactionSimulator.IsReactant( complex ))
                {
                    reactionSimulatorsList.Add( reactionSimulator );
                }
            }
            return reactionSimulatorsList.ToArray();
        }

        public void RegisterParticle (ParticleSimulator particleSimulator)
        {
            if (!particleSimulators.Contains( particleSimulator ))
            {
                particleSimulators.Add( particleSimulator );
            }
        }

        public void UnregisterParticle (ParticleSimulator particleSimulator)
        {
            if (particleSimulators.Contains( particleSimulator ))
            {
                particleSimulators.Remove( particleSimulator );
            }

            if (!particleSimulatorsToDestroy.Contains( particleSimulator ))
            {
                particleSimulatorsToDestroy.Add( particleSimulator );
            }
        }

        public void RegisterComplex (ComplexSimulator complexSimulator)
        {
            if (complexSimulator.couldReactOnCollision)
            {
                if (!complexSimulators.Contains( complexSimulator ))
                {
                    complexSimulators.Add( complexSimulator );
                }
            }
        }

        public void UnregisterComplex (ComplexSimulator complexSimulator)
        {
            if (complexSimulators.Contains( complexSimulator ))
            {
                complexSimulators.Remove( complexSimulator );
            }

            if (!complexSimulatorsToDestroy.Contains( complexSimulator ))
            {
                complexSimulatorsToDestroy.Add( complexSimulator );
            }
        }

        public void ComplexChangedCouldReactOnCollisionState (ComplexSimulator complexSimulator)
        {
            if (complexSimulator.couldReactOnCollision)
            {
                if (complexSimulators.Contains( complexSimulator ))
                {
                    complexSimulators.Remove( complexSimulator );
                }
            }
            else
            {
                if (!complexSimulators.Contains( complexSimulator ))
                {
                    complexSimulators.Add( complexSimulator );
                }
            }
        }

        void Update ()
        {
            #if UNITY_EDITOR
            if (Input.GetKeyDown( KeyCode.X ))
            {
                ObjectStateTests.StateOfReactorIsCorrect( this );
            }
            #endif

                //UnityEngine.Profiling.Profiler.BeginSample("MoveParticles");
            MoveParticles();
                //UnityEngine.Profiling.Profiler.EndSample();

            CalculateObservedRates();

                //UnityEngine.Profiling.Profiler.BeginSample("CollisionFreeReactions");
            DoCollisionFreeReactions();
                //UnityEngine.Profiling.Profiler.EndSample();

                //UnityEngine.Profiling.Profiler.BeginSample("BimolecularReactions");
            DoBimolecularReactions();
                //UnityEngine.Profiling.Profiler.EndSample();

            Cleanup();
        }

		void CalculateObservedRates ()
        {
            foreach (ReactionSimulator reactionSimulator in collisionFreeReactionSimulators)
            {
                reactionSimulator.CalculateObservedRate();
            }
            foreach (ReactionSimulator reactionSimulator in bimolecularReactionSimulators)
            {
                reactionSimulator.CalculateObservedRate();
            }
        }

        protected virtual void MoveParticles ()
        {
            foreach (ParticleSimulator particleSimulator in particleSimulators)
            {
                particleSimulator.Move( dT );
            }
        }

        public virtual bool WillCollide (ParticleSimulator particleSimulator, Vector3 newPosition)
        {
            foreach (ParticleSimulator otherParticleSimulator in particleSimulators)
            {
                if (particleSimulator.IsCollidingWith( otherParticleSimulator, newPosition ))
                {
                    return true;
                }
            }
            return false;
        }

        protected virtual void DoCollisionFreeReactions ()
        {
            //int start = collisionFreeReactionSimulators.GetRandomIndex();
            //for (int i = 0; i < collisionFreeReactionSimulators.Count; i++)
            //{
            //    collisionFreeReactionSimulators[(start + i) % collisionFreeReactionSimulators.Count].TryReact();
            //}

            collisionFreeReactionSimulators.Shuffle();
            foreach (CollisionFreeReactionSimulator collisionFreeReactionSimulator in collisionFreeReactionSimulators)
            {
                collisionFreeReactionSimulator.TryReact();
            }
        }

        protected virtual void DoBimolecularReactions ()
        {
            ComplexSimulator complexSimulator;
            int start = complexSimulators.GetRandomIndex();
            for (int i = 0; i < complexSimulators.Count; i++)
            {
                complexSimulator = complexSimulators[(start + i) % complexSimulators.Count];
                for (int j = i + 1; j < complexSimulators.Count; j++)
                {
                    complexSimulator.InteractWith( complexSimulators[(start + j) % complexSimulators.Count] );
                }
            }
        }

        protected virtual void Cleanup ()
        {
            foreach (ComplexSimulator complexSimulator in complexSimulatorsToDestroy)
            {
                Destroy( complexSimulator );
            }
            complexSimulatorsToDestroy.Clear();

            foreach (ParticleSimulator particleSimulator in particleSimulatorsToDestroy)
            {
                if (particleSimulator.GetComponent<MoleculeSimulator>())
                {
                    Destroy( particleSimulator );
                }
                else
                {
                    Destroy( particleSimulator.gameObject );
                }
            }
            particleSimulatorsToDestroy.Clear();
        }
    }
}