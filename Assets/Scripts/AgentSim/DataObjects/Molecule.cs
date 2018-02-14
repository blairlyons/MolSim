﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AICS.AgentSim
{
    public class Molecule : ScriptableObject
    {
        public string species;
        [Tooltip( "[scale] meters" )] 
        public float radius;
        [Tooltip( "conversion factor to meters" )] 
        public float scale;
        [Tooltip( "([scale] meters)^2 / s" )]
        public float diffusionCoefficient = 3e5f;
        public MoleculeComponent[] components = new MoleculeComponent[0];

        public GameObject _visualizationPrefab;
        public GameObject visualizationPrefab
        {
            get
            {
                if (_visualizationPrefab == null)
                {
                    _visualizationPrefab = Resources.Load( "DefaultMolecule" ) as GameObject;
                }
                return _visualizationPrefab;
            }
        }

        public Molecule (string _species, float _radius, float _scale, float _diffusionCoefficient, MoleculeComponent[] _components, GameObject _prefab = null)
        {
            species = _species;
            radius = _radius;
            scale = _scale;
            diffusionCoefficient = _diffusionCoefficient;
            components = _components;
            _visualizationPrefab = _prefab;
        }

        public MoleculeComponent GetComponentByID (string id)
        {
            foreach (MoleculeComponent component in components)
            {
                if (component.id == id)
                {
                    return component;
                }
            }
            return null;
        }
    }

    [System.Serializable]
    public class MoleculeComponent
    {
        public string id;
        public string[] states;

        public MoleculeComponent (string _id, string[] _states)
        {
            id = _id;
            states = _states;
        }
    }
}