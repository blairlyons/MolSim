﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AICS.AgentSim
{
    public class Reactor : MonoBehaviour 
    {
        [HideInInspector] public Container container;
        [HideInInspector] public Spawner spawner;
        public ModelDef modelDef;
        public List<BimolecularReaction> bimolecularReactions = new List<BimolecularReaction>();
        public List<CollisionFreeReaction> collisionFreeReactions = new List<CollisionFreeReaction>();
        [Tooltip( "How many attempts to move particles each frame? collisions and boundaries can cause move to fail" )]
        public int maxMoveAttempts = 20;
        [Tooltip( "Reflect particle to other side of container when it runs into a wall?" )]
        public bool periodicBoundary = true;

        public List<Mover> movers = new List<Mover>();
        public List<Complex> complexes = new List<Complex>();
        protected List<Mover> moversToDestroy = new List<Mover>();
        protected List<Complex> complexesToDestroy = new List<Complex>();

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
            modelDef.Init(); //for prototyping in inspector without writing custom property drawer etc

            foreach (ReactionDef reactionDef in modelDef.reactionDefs)
            {
                if (reactionDef.isBimolecular)
                {
                    bimolecularReactions.Add( new BimolecularReaction( reactionDef, this ) );
                }
                else
                {
                    collisionFreeReactions.Add( new CollisionFreeReaction( reactionDef, this ) );
                }
            }
        }

        protected virtual void CreateContainer ()
        {
            container = gameObject.AddComponent<Container>();
            container.Init( modelDef.scale, modelDef.containerVolume, periodicBoundary );
        }

        protected virtual void SpawnComplexes ()
        {
            spawner = gameObject.AddComponent<Spawner>();
            foreach (ComplexConcentration complex in modelDef.complexes)
            {
                spawner.SpawnComplexes( complex, this );
            }
        }

        public BimolecularReaction[] GetRelevantBimolecularReactions (ComplexSnapshot complexSnapshot)
        {
            List<BimolecularReaction> reactionsList = new List<BimolecularReaction>();
            foreach (BimolecularReaction reaction in bimolecularReactions)
            {
                if (reaction.ComplexIsReactant( complexSnapshot ))
                {
                    reactionsList.Add( reaction );
                }
            }
            return reactionsList.ToArray();
        }

        public BimolecularReaction[] GetRelevantBimolecularReactions (Molecule[] molecules)
        {
            List<BimolecularReaction> reactionsList = new List<BimolecularReaction>();
            foreach (BimolecularReaction reaction in bimolecularReactions)
            {
                if (reaction.ComplexIsReactant( molecules ))
                {
                    reactionsList.Add( reaction );
                }
            }
            return reactionsList.ToArray();
        }

        public CollisionFreeReaction[] GetRelevantCollisionFreeReactions (ComplexSnapshot complexSnapshot)
        {
            List<CollisionFreeReaction> reactionsList = new List<CollisionFreeReaction>();
            foreach (CollisionFreeReaction reaction in collisionFreeReactions)
            {
                if (reaction.ComplexIsReactant( complexSnapshot ))
                {
                    reactionsList.Add( reaction );
                }
            }
            return reactionsList.ToArray();
        }

        public CollisionFreeReaction[] GetRelevantCollisionFreeReactions (Molecule[] molecules)
        {
            List<CollisionFreeReaction> reactionsList = new List<CollisionFreeReaction>();
            foreach (CollisionFreeReaction reaction in collisionFreeReactions)
            {
                if (reaction.ComplexIsReactant( molecules ))
                {
                    reactionsList.Add( reaction );
                }
            }
            return reactionsList.ToArray();
        }

        public void RegisterMover (Mover mover)
        {
            if (!movers.Contains( mover ))
            {
                movers.Add( mover );
            }
        }

        public void UnregisterMover (Mover mover)
        {
            if (movers.Contains( mover ))
            {
                movers.Remove( mover );
            }

            if (!moversToDestroy.Contains( mover ))
            {
                moversToDestroy.Add( mover );
            }
        }

        public void RegisterComplex (Complex complex)
        {
            if (complex.couldReactOnCollision)
            {
                if (!complexes.Contains( complex ))
                {
                    complexes.Add( complex );
                }
            }
        }

        public void UnregisterComplex (Complex complex)
        {
            if (complexes.Contains( complex ))
            {
                complexes.Remove( complex );
            }

            if (!complexesToDestroy.Contains( complex ))
            {
                complexesToDestroy.Add( complex );
            }
        }

        public void ComplexChangedCouldReactOnCollisionState (Complex complex)
        {
            if (complex.couldReactOnCollision)
            {
                if (complexes.Contains( complex ))
                {
                    complexes.Remove( complex );
                }
            }
            else
            {
                if (!complexes.Contains( complex ))
                {
                    complexes.Add( complex );
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

                //UnityEngine.Profiling.Profiler.BeginSample("CollisionFreeReactions");
            DoCollisionFreeReactions();
                //UnityEngine.Profiling.Profiler.EndSample();

                //UnityEngine.Profiling.Profiler.BeginSample("BimolecularReactions");
            DoBimolecularReactions();
                //UnityEngine.Profiling.Profiler.EndSample();

            Cleanup();
        }

        protected virtual void MoveParticles ()
        {
            foreach (Mover mover in movers)
            {
                mover.Move( dT );
            }
        }

        public virtual bool WillCollide (Mover mover, Vector3 newPosition)
        {
            foreach (Mover otherMover in movers)
            {
                if (mover.IsCollidingWith( otherMover, newPosition ))
                {
                    return true;
                }
            }
            return false;
        }

        protected virtual void DoCollisionFreeReactions ()
        {
            foreach (Reaction reaction in collisionFreeReactions)
            {
                reaction.CalculateObservedRate();
            }

            collisionFreeReactions.Shuffle();
            foreach (CollisionFreeReaction collisionFreeReaction in collisionFreeReactions)
            {
                collisionFreeReaction.TryReact();
            }
        }

        protected virtual void DoBimolecularReactions ()
        {
            foreach (Reaction reaction in bimolecularReactions)
            {
                reaction.CalculateObservedRate();
            }

            Complex complex;
            int start = complexes.GetRandomIndex();
            for (int i = 0; i < complexes.Count; i++)
            {
                complex = complexes[(start + i) % complexes.Count];
                for (int j = i + 1; j < complexes.Count; j++)
                {
                    complex.InteractWith( complexes[(start + j) % complexes.Count] );
                }
            }
        }

        protected virtual void Cleanup ()
        {
            foreach (Complex complex in complexesToDestroy)
            {
                Destroy( complex );
            }
            complexesToDestroy.Clear();

            foreach (Mover mover in moversToDestroy)
            {
                Destroy( mover.gameObject );
            }
            moversToDestroy.Clear();
        }
    }
}