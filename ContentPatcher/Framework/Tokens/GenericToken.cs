using System;
using System.Collections.Generic;
using ContentPatcher.Framework.Tokens.ValueProviders;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Tokens
{
    /// <summary>A combination of one or more value providers.</summary>
    internal class GenericToken : IToken
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying value provider.</summary>
        protected IValueProvider Values { get; }

        /// <summary>Whether the root token may contain multiple values.</summary>
        protected bool CanHaveMultipleRootValues { get; set; }


        /*********
        ** Accessors
        *********/
        /// <summary>The mod namespace in which the token is accessible, or <c>null</c> for any namespace.</summary>
        public string Scope { get; }

        /// <summary>The token name.</summary>
        public virtual string Name { get; }

        /// <summary>Whether the value can change after it's initialized.</summary>
        public bool IsMutable => this.Values.IsMutable;

        /// <summary>Whether the instance is valid for the current context.</summary>
        public bool IsReady => this.Values.IsReady;

        /// <summary>Whether this token recognizes input arguments (e.g. <c>Relationship:Abigail</c> is a <c>Relationship</c> token with an <c>Abigail</c> input).</summary>
        public bool CanHaveInput => this.Values.AllowsInput;

        /// <summary>Whether this token is only valid with an input argument (see <see cref="IToken.CanHaveInput"/>).</summary>
        public bool RequiresInput => this.Values.RequiresInput;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="provider">The underlying value provider.</param>
        /// <param name="scope">The mod namespace in which the token is accessible, or <c>null</c> for any namespace.</param>
        public GenericToken(IValueProvider provider, string scope = null)
        {
            this.Values = provider;
            this.Scope = scope;
            this.Name = provider.Name;
            this.CanHaveMultipleRootValues = provider.CanHaveMultipleValues();
        }

        /// <summary>Update the token data when the context changes.</summary>
        /// <param name="context">The condition context.</param>
        /// <returns>Returns whether the token data changed.</returns>
        public virtual bool UpdateContext(IContext context)
        {
            return this.Values.UpdateContext(context);
        }

        /// <summary>Get the token names used by this patch in its fields.</summary>
        public virtual IEnumerable<string> GetTokensUsed()
        {
            return this.Values.GetTokensUsed();
        }

        /// <summary>Get diagnostic info about the contextual instance.</summary>
        public virtual IContextualState GetDiagnosticState()
        {
            return this.Values.GetDiagnosticState();
        }

        /// <summary>Whether the token may return multiple values for the given name.</summary>
        /// <param name="input">The input argument, if any.</param>
        public virtual bool CanHaveMultipleValues(ITokenString input)
        {
            return this.Values.CanHaveMultipleValues(input);
        }

        /// <summary>Validate that the provided input argument is valid.</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <param name="error">The validation error, if any.</param>
        /// <returns>Returns whether validation succeeded.</returns>
        public virtual bool TryValidateInput(ITokenString input, out string error)
        {
            return this.Values.TryValidateInput(input, out error);
        }

        /// <summary>Validate that the provided values are valid for the input argument (regardless of whether they match).</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <param name="values">The values to validate.</param>
        /// <param name="context">Provides access to contextual tokens.</param>
        /// <param name="error">The validation error, if any.</param>
        /// <returns>Returns whether validation succeeded.</returns>
        public virtual bool TryValidateValues(ITokenString input, InvariantHashSet values, IContext context, out string error)
        {
            if (!this.TryValidateInput(input, out error) || !this.Values.TryValidateValues(input, values, out error))
                return false;

            error = null;
            return true;
        }

        /// <summary>Get the allowed input arguments, if supported and restricted to a specific list.</summary>
        public virtual InvariantHashSet GetAllowedInputArguments()
        {
            return this.Values.GetValidInputs();
        }

        /// <summary>Get whether the token always chooses from a set of known values for the given input. Mutually exclusive with <see cref="IToken.HasBoundedRangeValues"/>.</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <param name="allowedValues">The possible values for the input.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="IToken.CanHaveInput"/> or <see cref="IToken.RequiresInput"/>.</exception>
        public virtual bool HasBoundedValues(ITokenString input, out InvariantHashSet allowedValues)
        {
            return this.Values.HasBoundedValues(input, out allowedValues);
        }

        /// <summary>Get whether the token always returns a value within a bounded numeric range for the given input. Mutually exclusive with <see cref="IToken.HasBoundedValues"/>.</summary>
        /// <param name="input">The input argument, if any.</param>
        /// <param name="min">The minimum value this token may return.</param>
        /// <param name="max">The maximum value this token may return.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="IToken.CanHaveInput"/> or <see cref="IToken.RequiresInput"/>.</exception>
        public virtual bool HasBoundedRangeValues(ITokenString input, out int min, out int max)
        {
            return this.Values.HasBoundedRangeValues(input, out min, out max);
        }

        /// <summary>Get the current token values.</summary>
        /// <param name="input">The input to check, if any.</param>
        /// <exception cref="InvalidOperationException">The input does not respect <see cref="IToken.CanHaveInput"/> or <see cref="IToken.RequiresInput"/>.</exception>
        public virtual IEnumerable<string> GetValues(ITokenString input)
        {
            this.AssertInput(input);
            return this.Values.GetValues(input);
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">The token name.</param>
        /// <param name="provider">The underlying value provider.</param>
        /// <param name="scope">The mod namespace in which the token is accessible, or <c>null</c> for any namespace.</param>
        protected GenericToken(string name, IValueProvider provider, string scope = null)
            : this(provider, scope)
        {
            this.Name = name;
        }

        /// <summary>Assert that an input argument is valid.</summary>
        /// <param name="input">The input to check, if any.</param>
        /// <exception cref="InvalidOperationException">The input does not respect <see cref="IToken.CanHaveInput"/> or <see cref="IToken.RequiresInput"/>.</exception>
        protected void AssertInput(ITokenString input)
        {
            bool hasInput = input.IsMeaningful();

            if (!this.CanHaveInput && hasInput)
                throw new InvalidOperationException($"The '{this.Name}' token does not allow input arguments ({InternalConstants.InputArgSeparator}).");
            if (this.RequiresInput && !hasInput)
                throw new InvalidOperationException($"The '{this.Name}' token requires an input argument.");
        }
    }
}
