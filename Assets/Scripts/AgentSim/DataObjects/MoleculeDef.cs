﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AICS.AgentSim
{
    public class MoleculeDef : ScriptableObject
    {
        [SerializeField] string _species = "";
        public string species
        {
            get
            {
                return _species;
            }
        }

        [Tooltip( "[scale] meters" )] 
        public float radius;
        [Tooltip( "conversion factor to meters" )] 
        public float scale;
        [Tooltip( "([scale] meters)^2 / s" )]
        public float diffusionCoefficient = 3e5f;
        public Dictionary<string,List<BindingSiteDef>> bindingSiteDefs;

        #region for prototyping in inspector without writing custom property drawer etc
        [SerializeField] BindingSiteDef[] siteDefs = new BindingSiteDef[0];
        public MoleculeSnapshotColor[] colors;

        public void Init ()
        {
            bindingSiteDefs = new Dictionary<string, List<BindingSiteDef>>();
            foreach (BindingSiteDef siteDef in siteDefs)
            {
                if (!bindingSiteDefs.ContainsKey( siteDef.id ))
                {
                    bindingSiteDefs.Add( siteDef.id, new List<BindingSiteDef>() );
                }
                bindingSiteDefs[siteDef.id].Add( siteDef );
            }
            if (colors != null)
            {
                foreach (MoleculeSnapshotColor color in colors)
                {
                    color.snapshot.InitSiteStates();
                }
            }
        }
        #endregion

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

        public override bool Equals (object obj)
        {
            MoleculeDef other = obj as MoleculeDef;
            if (other != null)
            {
                return other.species == species;
            }
            return false;
        }

        public override int GetHashCode ()
        {
            unchecked
            {
                return 16777619 * (species == null ? 0 : species.GetHashCode());
            }
        }

		public override string ToString()
		{
            return "molecule " + species;
		}
	}

    [System.Serializable]
    public class BindingSiteDef
    {
        public string id;
        public string[] states;
        public RelativeTransform transformOnMolecule;
        public float radius;
    }

    [System.Serializable]
    public class RelativeTransform
    {
        public Vector3 position;
        public Vector3 rotation;

        public RelativeTransform (Vector3 _position, Vector3 _rotation)
        {
            position = _position;
            rotation = _rotation;
        }

        public void Apply (Transform parent, Transform child)
        {
            child.position = parent.TransformPoint( position );
            child.rotation = parent.rotation * Quaternion.Euler( rotation );
        }
    }

    [System.Serializable]
    public class MoleculeSnapshotColor
    {
        public MoleculeSnapshot snapshot;
        public Color color;
    }
}