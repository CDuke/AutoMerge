// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace AutoMerge.Prism.Events
{
    ///<summary>
    /// Extends <see cref="EventSubscription{TPayload}"/> to invoke the <see cref="EventSubscription{TPayload}.Action"/> delegate
    /// in a specific <see cref="SynchronizationContext"/>.
    ///</summary>
    /// <typeparam name="TPayload">The type to use for the generic <see cref="System.Action{TPayload}"/> and <see cref="Predicate{TPayload}"/> types.</typeparam>
    public class DispatcherEventSubscription<TPayload> : EventSubscription<TPayload>
    {
        private readonly SynchronizationContext _syncContext;

        ///<summary>
        /// Creates a new instance of <see cref="BackgroundEventSubscription{TPayload}"/>.
        ///</summary>
        ///<param name="actionReference">A reference to a delegate of type <see cref="System"/>.</param>
        ///<param name="filterReference">A reference to a delegate of type <see cref="Predicate{TPayload}"/>.</param>
        ///<param name="context">The synchronization context to use for UI thread dispatching.</param>
        ///<exception cref="ArgumentNullException">When <paramref name="actionReference"/> or <see paramref="filterReference"/> are <see langword="null" />.</exception>
        ///<exception cref="ArgumentException">When the target of <paramref name="actionReference"/> is not of type <see cref="System"/>,
        ///or the target of <paramref name="filterReference"/> is not of type <see cref="BackgroundEventSubscription{TPayload}"/>.</exception>
        public DispatcherEventSubscription(IDelegateReference actionReference, IDelegateReference filterReference, SynchronizationContext context)
            : base(actionReference, filterReference)
        {
            _syncContext = context;
        }

        /// <summary>
        /// Invokes the specified <see cref="System.Action{TPayload}"/> asynchronously in the specified <see cref="SynchronizationContext"/>.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="argument">The payload to pass <paramref name="action"/> while invoking it.</param>
        public override void InvokeAction(Action<TPayload> action, TPayload argument)
        {
            _syncContext.Post((o) => action((TPayload)o), argument);
        }
    }
}
