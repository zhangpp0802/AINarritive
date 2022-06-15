﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Assets.SAT
{
    /// <summary>
    /// Represents a SAT problem
    /// </summary>
    public class Problem
    {
        /// <summary>
        /// Make a problem from the constraints specified in the file
        /// The format of the file is:
        /// - 1 line per constraint
        /// - ! means not, | means or
        /// - proposition names can be anything not including those characters
        /// </summary>
        /// <param name="path">Path to the file to load</param>
        public Problem(string path)
        {
            Constraints.AddRange(File.ReadAllLines(path).Where(s => !s.StartsWith("#")).Select(constraintExp => Constraint.FromExpression(constraintExp, this)));
            TrueLiteralCounts = new int[Constraints.Count];

            Solution = new TruthAssignment(this);

            for (var i = 0; i < Constraints.Count; i++)
                TrueLiteralCounts[i] = Solution.TrueLiteralCount(Constraints[i]);

            UnsatisfiedConstraints.AddRange(Constraints.Where(Unsatisfied));
        }
        
        /// <summary>
        /// The truth assignment we are trying to make into a solution.
        /// This starts completely random is then is gradually changed into
        /// a solution by "flipping" the values of specific propositions
        /// </summary>
        public TruthAssignment Solution;

        /// <summary>
        /// Percent of the time we do a random walk step rather than a greedy one.
        /// 0   = pure greedy
        /// 100 = pure random walk
        /// </summary>
        public int NoiseLevel = 10;

        #region Proposition information
        /// <summary>
        /// The Proposition object within this problem with the specified name.
        /// Creates a new proposition object if necessary.
        /// </summary>
        public Proposition this[string name]
        {
            get
            {
                if (propositionTable.TryGetValue(name, out var result))
                    return result;
                return propositionTable[name ] = new Proposition(name, propositionTable.Count);
            }
        }

        /// <summary>
        /// Hash table mapping names (string) to the Proposition objects with that name
        /// </summary>
        private readonly Dictionary<string, Proposition> propositionTable = new Dictionary<string, Proposition>();

        /// <summary>
        /// Enumeration of all the propositions in the problem
        /// </summary>
        public IEnumerable<Proposition> Propositions => propositionTable.Select(pair => pair.Value);

        /// <summary>
        /// Total number of propositions in the problem.
        /// Note this is the number of propositions, not the number of disjuncts in constraints.
        /// If a Proposition appears in 3 constraints, it's only counted once here.
        /// </summary>
        public int PropositionCount => propositionTable.Count;

        /// <summary>
        /// True if the current value of Solution is in fact a solution.
        /// If it's false, then we need to work on it some more.
        /// </summary>
        public bool IsSolved => UnsatisfiedConstraints.Count == 0;
        #endregion

        #region constraint information
        /// <summary>
        /// Constraints in the SAT problem.
        /// </summary>
        public readonly List<Constraint> Constraints = new List<Constraint>();

        /// <summary>
        /// List of constraints whose number of literals is not in its required range,
        /// i.e. MinTrueLiterals-MaxTrueLiterals.  The solver needs to get the number
        /// of true literals for each constraint into the right range.
        /// </summary>
        public readonly List<Constraint> UnsatisfiedConstraints = new List<Constraint>();

        /// <summary>
        /// Number of literals in each constraint that are true, indexed by the Index field of the constraint.
        /// So to find out how many literals are true in c, look at TrueLiteralCounts[c.Index].
        /// </summary>
        public int[] TrueLiteralCounts;

        /// <summary>
        /// Number of literals that are true within the constraint given the current TruthAssignment
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public int CurrentTrueLiterals(Constraint c) => TrueLiteralCounts[c.Index];

        /// <summary>
        /// True if the specified constraint is currently satisfied
        /// (i.e. if it's true in the current truth assignment)
        /// </summary>
        public bool Satisfied(Constraint c) => CurrentTrueLiterals(c) >= c.MinTrueLiterals && CurrentTrueLiterals(c) <= c.MaxTrueLiterals;

        /// <summary>
        /// True if the specified constraint is currently unsatisfied
        /// (i.e. false in the current truth assignment).
        /// </summary>
        public bool Unsatisfied(Constraint c) => !Satisfied(c);
        
        /// <summary>
        /// Checks that the TrueLiteralCounts array and UnsatisfiedConstraints list are correct.
        /// Use this to look for bugs in your implementation of Flip.
        /// </summary>
        public void CheckConsistency()
        {
            for (var i = 0; i < Constraints.Count; i++)
                if (TrueLiteralCounts[i] != Solution.TrueLiteralCount(Constraints[i]))
                    throw new Exception($"True literal count incorrect for constraint {i}");
            foreach (var c in Constraints)
            {
                var present = UnsatisfiedConstraints.IndexOf(c) >= 0;
                if (Satisfied(c))
                {
                    if (present)
                        throw new Exception($"constraint \"{c}\" appears in UnsatisfiedConstraints but is satisfied.  Last flip was {lastFlip}");
                }
                else if (!present)
                    throw new Exception($"constraint \"{c}\" is unsatisfied but does not appear in the UnsatisfiedConstraints list.  Last flip was {lastFlip}");
            }
        }
        #endregion

        #region Solver
        /// <summary>
        /// Pick one variable to flip and flip it by calling Flip, below.
        /// </summary>
        /// <returns>True if all constraints are satisfied.</returns>
        public bool StepOne()
        {
            if (UnsatisfiedConstraints.Count != 0){
                // get the random Constraint
                Constraint c = UnsatisfiedConstraints.RandomElement();

                // get the random literal and flip()
                Literal lit = c.RandomLiteral;

                // see best literals in the randomed constraint
                if(!Random.Percent(NoiseLevel)){ // when noise is still small
                    var bestLit = c.Literals[0];
                    foreach (var currentLit in c.Literals)
                    {
                        if (SatisfiedConstraintDelta(currentLit.Proposition) > SatisfiedConstraintDelta(bestLit.Proposition))
                        {
                            bestLit = currentLit;
                        }
                    }
                    lit = bestLit;
                    
                    if(lastFlip != null){
                        int oldC = SatisfiedConstraintDelta(lastFlip.Proposition);
                        int newC = SatisfiedConstraintDelta(lit.Proposition);
                        if(oldC>newC){ // not making progress, add noise
                            if (NoiseLevel<100){
                                NoiseLevel +=1;
                            }
                        }
                        else{ //making process, reduce noise
                            if (NoiseLevel>0){
                                NoiseLevel-=1;
                            }
                        }
                    }
                }

                Flip(lit);
                return false;
                // flip a random literal in random unsatisfiedConstraint
            }
            else{
                return true;
            }
        }

        private Literal lastFlip;
        /// <summary>
        /// Flip the value of the specified literal.
        /// Call Solution.Flip to do the actual flipping.  But make sure to update
        /// TrueLiteralCounts and UnsatisfiedConstraints, accordingly
        /// </summary>
        void Flip(Literal l)
        {
            lastFlip = l;
            var p = l.Proposition;
            var old = Solution[p];
            UnityEngine.Debug.Log($"{p.Name}: {old}->{!old}");
            Solution.Flip(p);

            // TODO: Fill this in!
            List<Constraint> pos = p.PositiveConstraints;
            List<Constraint> neg = p.NegativeConstraints;
            if (Solution[p]){ // F to T
                foreach (Constraint c in pos){
                    //find index of c in constraints and change the trueCount; add to unsatisfy when overbound
                    TrueLiteralCounts[c.Index] = TrueLiteralCounts[c.Index]+1;
                }
                foreach (Constraint c in neg){
                    //find index of c in constraints and change the trueCount; add to unsatisfy when overbound
                    TrueLiteralCounts[c.Index] = TrueLiteralCounts[c.Index]-1;
                }
            }

            else{ // T to F
                foreach (Constraint c in pos){
                    //find index of c in constraints and change the trueCount; add to unsatisfy when overbound
                    TrueLiteralCounts[c.Index] = TrueLiteralCounts[c.Index]-1;
                }
                foreach (Constraint c in neg){
                    //find index of c in constraints and change the trueCount; add to unsatisfy when overbound
                    TrueLiteralCounts[c.Index] = TrueLiteralCounts[c.Index]+1;
                }
            }
            // if (TrueLiteralCounts[c.Index] < c.MinTrueLiterals){
            //     UnsatisfiedConstraints.Add(c);
            // }
            // else if (TrueLiteralCounts[c.Index] > c.MaxTrueLiterals){
            //     UnsatisfiedConstraints.Add(c);
            // }
            // else{
            //     UnsatisfiedConstraints.Remove(c);
            // }
            foreach (var c in Constraints)
            {
                if (Satisfied(c))
                {
                    if (UnsatisfiedConstraints.Contains(c))
                    {
                        UnsatisfiedConstraints.Remove(c);
                    }
                }
                else
                {
                    if (!UnsatisfiedConstraints.Contains(c))
                    {
                        UnsatisfiedConstraints.Add(c);
                    }
                }
            }
        }

        /// <summary>
        /// Return the net increase or decrease in satisfied constraints if we were to flip this proposition
        /// </summary>
        int SatisfiedConstraintDelta(Proposition p)
        {
            var previousUnsatisfy = UnsatisfiedConstraints.Count();
            Solution.Flip(p);
            var currentUnsatisfy = UnsatisfiedConstraints.Count();
            Solution.Flip(p);
            return previousUnsatisfy-currentUnsatisfy;
        }
        #endregion
    }
}
