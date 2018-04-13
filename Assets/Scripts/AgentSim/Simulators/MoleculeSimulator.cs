﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AICS.AgentSim
{
    public class MoleculeSimulator : MonoBehaviour 
    {
        public ComplexSimulator complexSimulator;
        public Molecule molecule;
        public Dictionary<string,BindingSiteSimulator> bindingSiteSimulators = new Dictionary<string,BindingSiteSimulator>();
        public float collisionRadius;
        public float interactionRadius;

        Transform _theTransform;
        public Transform theTransform
        {
            get
            {
                if (_theTransform == null)
                {
                    _theTransform = transform;
                }
                return _theTransform;
            }
        }

        public bool couldReactOnCollision;

        bool GetCouldReactOnCollision ()
        {
            foreach (BindingSiteSimulator bindingSiteSimulator in bindingSiteSimulators.Values)
            {
                if (bindingSiteSimulator.couldReactOnCollision)
                {
                    return true;
                }
            }
            return false;
        }

        public string species
        {
            get
            {
                return molecule.species;
            }
        }

        public virtual void Init (MoleculeState moleculeState, ComplexSimulator _complexSimulator, 
                                  BimolecularReactionSimulator[] relevantBimolecularSimulators, CollisionFreeReactionSimulator[] relevantCollisionFreeSimulators)
        {
            complexSimulator = _complexSimulator;
            molecule = moleculeState.molecule;
            collisionRadius = interactionRadius = molecule.radius;
            interactionRadius += 1f;
            CreateBindingSites( moleculeState, relevantBimolecularSimulators, relevantCollisionFreeSimulators );
            couldReactOnCollision = GetCouldReactOnCollision();
        }

        protected virtual void CreateBindingSites (MoleculeState moleculeState, BimolecularReactionSimulator[] relevantBimolecularSimulators, 
                                                   CollisionFreeReactionSimulator[] relevantCollisionFreeSimulators)
        {
            foreach (string bindingSiteID in molecule.bindingSites.Keys)
            {
                CreateBindingSite( bindingSiteID, moleculeState, relevantBimolecularSimulators, relevantCollisionFreeSimulators );
            }
        }

        protected virtual void CreateBindingSite (string bindingSiteID, MoleculeState moleculeState, 
                                                  BimolecularReactionSimulator[] relevantBimolecularSimulators, CollisionFreeReactionSimulator[] relevantCollisionFreeSimulators)
        {
            GameObject bindingSiteObject = new GameObject();
            bindingSiteObject.transform.SetParent( theTransform );
            molecule.bindingSites[bindingSiteID].transformOnMolecule.Apply( theTransform, bindingSiteObject.transform );
            bindingSiteObject.name = name + "_" + bindingSiteID;

            BindingSiteSimulator bindingSiteSimulator = bindingSiteObject.AddComponent<BindingSiteSimulator>();
            bindingSiteSimulator.Init( bindingSiteID, moleculeState, relevantBimolecularSimulators, relevantCollisionFreeSimulators, this );

            bindingSiteSimulators.Add( bindingSiteID, bindingSiteSimulator );
        }

        public virtual bool InteractWith (MoleculeSimulator other)
        {
            foreach (BindingSiteSimulator bindingSiteSimulator in bindingSiteSimulators.Values)
            {
                if (bindingSiteSimulator.couldReactOnCollision)
                {
                    foreach (BindingSiteSimulator otherBindingSiteSimulator in other.bindingSiteSimulators.Values)
                    {
                        if (otherBindingSiteSimulator.couldReactOnCollision && bindingSiteSimulator.ReactWith( otherBindingSiteSimulator ))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public virtual void MoveToComplex (ComplexSimulator _complexSimulator, BimolecularReactionSimulator[] relevantBimolecularSimulators, 
                                           CollisionFreeReactionSimulator[] relevantCollisionFreeSimulators)
        {
            complexSimulator.Remove( this );
            complexSimulator = _complexSimulator;
            name = complexSimulator.name + "_" + species;
            theTransform.SetParent( complexSimulator.theTransform );

            UpdateReactions( relevantBimolecularSimulators, relevantCollisionFreeSimulators );
        }

        public virtual void UpdateReactions (BimolecularReactionSimulator[] relevantBimolecularSimulators, CollisionFreeReactionSimulator[] relevantCollisionFreeSimulators)
        {
            foreach (BindingSiteSimulator bindingSiteSimulator in bindingSiteSimulators.Values)
            {
                bindingSiteSimulator.UpdateReactions( relevantBimolecularSimulators, relevantCollisionFreeSimulators );
            }
            couldReactOnCollision = GetCouldReactOnCollision();
        }

        public override string ToString ()
        {
            return "MoleculeSimulator " + name;
        }

        Material _material;
        Material material
        {
            get
            {
                if (_material == null)
                {
                    _material = GetComponent<MeshRenderer>().material;
                }
                return _material;
            }
        }

        public void SetColor (Color color)
        {
            material.color = color;
        }
	}
}