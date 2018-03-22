﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AICS.AgentSim
{
    [System.Serializable]
	public class CollisionFreeReactionWatcher : ReactionWatcher
	{
        [SerializeField] List<BindingSitePopulation> populations = new List<BindingSitePopulation>();

		public CollisionFreeReactionWatcher (Reaction _reaction) : base (_reaction) { }

        public void RegisterBindingSitePopulation (BindingSitePopulation bindingSitePopulation, ComplexState complex)
        {
            if (!populations.Contains( bindingSitePopulation ) && ComplexIsReactant( complex ) 
                && bindingSitePopulation.moleculeBindingSite.Matches( reaction.relevantSites[0] ))
            {
                populations.Add( bindingSitePopulation );
            }
        }

        bool ComplexIsReactant (ComplexState complexState)
        {
            foreach (ComplexState reactant in reaction.reactantStates)
            {
                if (reactant.Matches( complexState ))
                {
                    return true;
                }
            }
            return false;
        }

        public void UnregisterBindingSitePopulation (BindingSitePopulation bindingSitePopulation)
        {
            if (populations.Contains( bindingSitePopulation ))
            {
                populations.Remove( bindingSitePopulation );
            }
        }

        public bool TryReact ()
        {
            if (populations.Count > 0 && shouldHappen)
            {
                populations.Shuffle();
                populations[0].DoCollisionFreeReaction( reaction );
            }
            return false;
        }
	}

    [System.Serializable]
	public class BimolecularReactionWatcher : ReactionWatcher
	{
		public BimolecularReactionWatcher (Reaction _reaction) : base (_reaction) { }

        public bool TryReactOnCollision (BindingSiteSimulator bindingSiteSimulator1, BindingSiteSimulator bindingSiteSimulator2)
        {
            if (ReactantsEqual( bindingSiteSimulator1.complex, bindingSiteSimulator2.complex ))
            {
                return shouldHappen;
            }
            return false;
        }

        bool ReactantsEqual (List<MoleculeSimulator> complex1, List<MoleculeSimulator> complex2)
        {
            return (reaction.reactantStates.Length == 0 && complex1 == null && complex2 == null)
                || (reaction.reactantStates.Length == 1 && ((reaction.reactantStates[0].Matches( complex1 ) && complex2 == null)
                                                         || (reaction.reactantStates[0].Matches( complex2 ) && complex1 == null)))
                || (reaction.reactantStates.Length == 2 && ((reaction.reactantStates[0].Matches( complex1 ) && reaction.reactantStates[1].Matches( complex2 )))
                                                         || (reaction.reactantStates[0].Matches( complex2 ) && reaction.reactantStates[1].Matches( complex1 )));
        }
	}

	// runtime data for a reaction used to keep rate near its theoretical value
    [System.Serializable]
    public abstract class ReactionWatcher
    {
        public Reaction reaction;
        
        public int attempts;
        public int events;
        public float observedRate;

        public ReactionWatcher (Reaction _reaction)
        {
            reaction = _reaction;
        }

        public void CalculateObservedRate ()
        {
            observedRate = events / World.Instance.time;
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