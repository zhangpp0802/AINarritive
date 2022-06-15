﻿using System;

namespace HornClause
{
    /// <summary>
    /// Methods to implement unification of variables
    /// </summary>
    public static class Unifier
    {
        /// <summary>
        /// Return the final value of a variable within a substitution.  It's defined as follows (constant
        /// here just means anything other than a Variable):
        /// - Dereference(constant, subst) = constant
        /// - Dereference(variable, subst) = Dereference(variable's value in subst, subst)
        ///
        /// Note that this means Dereference will only ever return a constant or a variable that doesn't
        /// have a value in the substitution, aka an unbound variable.
        /// </summary>
        public static object Dereference(object term, Substitution subst)
        {
            if (term is Variable v){
                if (Substitution.Lookup(subst,v, out var vValue))
                {
                    return Dereference(vValue, subst);
                }
                else
                {
                    return v;
                }
            }
            else {
                return term;
            }
        }

        /// <summary>
        /// Test if it's possible to make the dereferenced versions of the two values the same, possibly by extending
        /// the substitution.  Output the Substitution that makes them the same.  If the dereferenced versions are already
        /// the same, then the unifiedSubst will just be the original one.  If they're not the same, but one of them is
        /// a variable, then the unifiedSubst will be the original subst extended with the variable set to the other
        /// dereferenced value.
        ///
        /// If the dereferenced values are different constants, they can't be made the same, and unify should return false.
        /// </summary>
        /// <param name="a">The first value to compare</param>
        /// <param name="b">The second value to compare</param>
        /// <param name="subst">The substitution currently in use</param>
        /// <param name="unifyingSubst">The extension of subst that makes a and b the same (this may just be the original subst)</param>
        /// <returns></returns>
        public static bool Unify(object a, object b, Substitution subst, out Substitution unifyingSubst)
        {
            object deA = Dereference(a, subst);
            object deB = Dereference(b, subst);
            if (deA.Equals(deB))
            {
                unifyingSubst = subst;
                return true;
            }
            else if(deA is Variable v)
            {
                unifyingSubst = new Substitution(v, deB, subst);
                return true;
            }
            else if (deB is Variable v2)
            {
                unifyingSubst = new Substitution(v2, deA, subst);
                return true;
            }
            else
            {
                unifyingSubst = null;
                return false;
            }
        }

        /// <summary>
        /// Check if a[0] can be unified with b[0], a[1] with b[1], etc.  Output the substitution needed to make
        /// each pair unify.
        /// </summary>
        public static bool UnifyArrays(object[] a, object[] b, Substitution subst, out Substitution unifyingSubst)
        {
            if (a.Length != b.Length)
            {
                unifyingSubst = null;
                return false;
            }
            else
            {
                for (int i = 0; i < a.Length; i++)
                {
                    if (!Unify(a[i], b[i], subst, out unifyingSubst))
                    {
                        return false;
                    }
                    subst = unifyingSubst;
                }
                unifyingSubst = subst;
                return true;
            }

        }
    }
}
