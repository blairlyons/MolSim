﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AICS.AgentSim
{
    public class Reaction : ScriptableObject
    {
        public string description;
        [Tooltip( "per second" )] 
        public float rate;
        public IReactable[] reactants;
        public IReactable[] products;

        public Reaction (IReactable[] _reactants, IReactable[] _products, float _rate, string _description = "")
        {
            description = _description;
            reactants = _reactants;
            products = _products;
            rate = _rate;
        }

        public bool ReactantsEqual (IReactable species1, IReactable species2)
        {
            return (reactants.Length == 0 && species1 == null && species2 == null)
                || (reactants.Length == 1 && ((species1.Matches( reactants[0] ) && species2 == null) 
                                           || (species2.Matches( reactants[0] ) && species1 == null)))
                || (reactants.Length == 2 && ((species1.Matches( reactants[0] ) && species2.Matches( reactants[1] ))
                                           || (species2.Matches( reactants[0] ) && species1.Matches( reactants[1] ) )));
        }
    }

    [System.Serializable]
    public class MoleculeState : IReactable
    {
        public Molecule molecule;
        public Dictionary<string,string> componentStates = new Dictionary<string,string>();

        public MoleculeState () { }

        public MoleculeState (Molecule _molecule, Dictionary<string,string> _componentStates)
        {
            molecule = _molecule;
            componentStates = _componentStates;
        }

        public bool Matches (IReactable other)
        {
            MoleculeState otherSingleMoleculeState = other as MoleculeState;
            if (otherSingleMoleculeState != null && otherSingleMoleculeState.molecule.species == molecule.species)
            {
                foreach (KeyValuePair<string,string> otherComponent in otherSingleMoleculeState.componentStates)
                {
                    if (componentStates[otherComponent.Key] != otherComponent.Value)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }

    [System.Serializable]
    public class CompoundMoleculeState : IReactable
    {
        public MoleculeState parentMoleculeState;
        public MoleculeState childMoleculeState;
        public Vector3 relativePosition;
        public Vector3 relativeRotation;

        public CompoundMoleculeState (MoleculeState _parentMoleculeState, MoleculeState _childMoleculeState, Vector3 _relativePosition, Vector3 _relativeRotation)
        {
            parentMoleculeState = _parentMoleculeState;
            childMoleculeState = _childMoleculeState;
            relativePosition = _relativePosition;
            relativeRotation = _relativeRotation;
        }

        public bool Matches (IReactable other)
        {
            CompoundMoleculeState otherCompoundMoleculeState = other as CompoundMoleculeState;
            return otherCompoundMoleculeState != null 
                && otherCompoundMoleculeState.parentMoleculeState.Matches( parentMoleculeState )
                && otherCompoundMoleculeState.childMoleculeState.Matches( childMoleculeState );
        }
    }
}