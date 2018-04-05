﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AICS.AgentSim
{
    [System.Serializable]
    public class CollisionFreeReactionSimulator : ReactionSimulator
	{
        [SerializeField] List<BindingSiteSimulator> bindingSiteSimulators = new List<BindingSiteSimulator>();

        public CollisionFreeReactionSimulator (Reaction _reaction) : base (_reaction) { }

        public bool Register (BindingSiteSimulator bindingSitesimulator, ComplexState complexState = null)
        {
            if (!bindingSiteSimulators.Contains( bindingSitesimulator ))
            {
                if ((complexState == null || IsReactant( complexState )) && SiteIsRelevant( bindingSitesimulator ))
                {
                    bindingSiteSimulators.Add( bindingSitesimulator );
                    return true;
                }
            }
            else
            {
                Debug.LogWarning( "Trying to register " + bindingSitesimulator + " but it's already registered!" );
            }
            return false;
        }

        public void Unregister (BindingSiteSimulator bindingSitesimulator)
        {
            if (bindingSiteSimulators.Contains( bindingSitesimulator ))
            {
                bindingSiteSimulators.Remove( bindingSitesimulator );
            }
            else
            {
                Debug.LogWarning( "Trying to remove " + bindingSitesimulator + " but it's not registered!" );
            }
        }

        public bool TryReact ()
        {
            if (bindingSiteSimulators.Count > 0 && shouldHappen)
            {
                bindingSiteSimulators.Shuffle();
                reaction.React( bindingSiteSimulators[0] );
                return true;
            }
            return false;
        }
	}

    [System.Serializable]
    public class BimolecularReactionSimulator : ReactionSimulator
	{
        public BimolecularReactionSimulator (Reaction _reaction) : base (_reaction) { }

        public bool TryReactOnCollision (BindingSiteSimulator bindingSiteSimulator1, BindingSiteSimulator bindingSiteSimulator2)
        {
            if (ReactantsEqual( bindingSiteSimulator1.complex, bindingSiteSimulator2.complex ) && shouldHappen)
            {
                reaction.React( bindingSiteSimulator1, bindingSiteSimulator2 );
                return true;
            }
            return false;
        }

        bool ReactantsEqual (MoleculeSimulator[] complex1, MoleculeSimulator[] complex2)
        {
            return ((reaction.reactantStates[0].IsSatisfiedBy( complex1 ) && reaction.reactantStates[1].IsSatisfiedBy( complex2 )))
                 || (reaction.reactantStates[0].IsSatisfiedBy( complex2 ) && reaction.reactantStates[1].IsSatisfiedBy( complex1 ));
            
        }
	}

	// runtime data for a reaction used to keep rate near its theoretical value
    [System.Serializable]
    public abstract class ReactionSimulator
    {
        public Reaction reaction;
        
        [SerializeField] int attempts;
        public int events;
        [SerializeField] float observedRate;

        public ReactionSimulator (Reaction _reaction)
        {
            reaction = _reaction;
        }

        public void CalculateObservedRate ()
        {
            observedRate = events / World.Instance.time;
        }

        public bool IsReactant (ComplexState complexState)
        {
            foreach (ComplexState reactant in reaction.reactantStates)
            {
                if (reactant.IsSatisfiedBy( complexState ))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsReactant (MoleculeSimulator[] complex)
        {
            foreach (ComplexState reactantState in reaction.reactantStates)
            {
                if (reactantState.IsSatisfiedBy( complex ))
                {
                    return true;
                }
            }
            return false;
        }

        public bool SiteIsRelevant (BindingSiteSimulator bindingSiteSimulator)
        {
            foreach (MoleculeBindingSite site in reaction.relevantSites)
            {
                if (site.Matches( bindingSiteSimulator.molecule, bindingSiteSimulator.id ))
                {
                    return true;
                }
            }
            return false;
        }

        bool observedRateTooHigh
        {
            get
            {
                return observedRate > 1.2f * reaction.rate;
            }
        }

        bool observedRateTooLow
        {
            get
            {
                return observedRate < 0.8f * reaction.rate;
            }
        }

        protected bool shouldHappen
        {
            get
            {
                attempts++;

                bool react;
                if (observedRateTooHigh)
                {
                    react = false;
                }
                else if (observedRateTooLow)
                {
                    react = true;
                }
                else 
                {
                    react = Random.value <= reaction.rate * World.Instance.dT * (World.Instance.steps / attempts);
                }

                events = react ? events + 1 : events;

                return react;
            }
        }

        public void Reset ()
        {
            events = attempts = 0;
            observedRate = 0;
        }
    }
}