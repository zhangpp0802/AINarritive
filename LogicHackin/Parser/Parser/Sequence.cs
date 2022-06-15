using System;

namespace Parser
{
    /// <summary>
    /// A parser that matches a sequence of categories
    /// For example, NP VP will match a string that starts with an NP and ends with a VP.
    /// </summary>
    public class Sequence : Category
    {
        /// <summary>
        /// Sequence of constituents (lexical and/or phrasal categories) to match
        /// If constituents are NP, VP then we match strings that start with an NP and end with a VP
        /// </summary>
        private readonly Category[] constituents;
        //private int na;

        /// <summary>
        /// Make a parser that recognizes sequences of the specified constituents
        /// </summary>
        /// <param name="name">Name for this sequence</param>
        /// <param name="constituents">Sequence of constituents to match</param>
        public Sequence(string name, params Category[] constituents) : base(name)
        {
            this.constituents = constituents;
            //this.trace = 0;
        }

        /// <summary>
        /// Match WordList to our sequence of constituents, then call continuation with array of parses from the constituents and the remaining words
        /// </summary>
        /// <param name="w">Words to parse</param>
        /// <param name="k">Continuation to call if successful</param>
        /// <returns>Result of k, or false if can't match</returns>
        public override bool Parse(WordList w, Continuation k)
        {
            var words = new object[constituents.Length+1];
            words[0] = this;
            return ParseMid(w, k, words, 0);
        }

        /// <summary>
        /// Recursive helper for sequence parser
        /// </summary>
        /// <param name="w">Words to parse</param>
        /// <param name="k">Continuation to call if successful</param>
        /// <returns>Result of k, or false if can't match</returns>
        private bool ParseMid(WordList w, Continuation k, object[] words,int i)
        {
            if (i == constituents.Length)
            {
                return k(words,w);
            }
            return constituents[i].Parse(w,(first,rest) =>
            {
                words[i + 1] = first;
                return ParseMid(rest, k, words, i + 1);
            });
        }
    }
}
