/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using OpenMetaverse;

namespace OpenSim.Framework
{
    public interface ISceneViewer
    {
        /// <summary>
        /// The instance of the prioritizer the SceneViewer uses
        /// </summary>
        IPrioritizer Prioritizer { get; }

        /// <summary>
        /// Add the objects to the queue for which we need to send an update to the client
        /// </summary>
        /// <param name="part"></param>
        void QueuePartForUpdate(ISceneChildEntity part, PrimUpdateFlags UpdateFlags);

        /// <summary>
        /// This loops through all of the lists that we have for the client
        ///  as well as checking whether the client has ever entered the sim before
        ///  and sending the needed updates to them if they have just entered.
        /// </summary>
        void SendPrimUpdates();

        /// <summary>
        /// Clear the updates for this part in the next update loop
        /// </summary>
        /// <param name="part"></param>
        void ClearUpdatesForPart (ISceneChildEntity sceneObjectPart);

        /// <summary>
        /// Clear the updates for this part in the next update loop only
        /// </summary>
        /// <param name="part"></param>
        void ClearUpdatesForOneLoopForPart (ISceneChildEntity sceneObjectPart);

        /// <summary>
        /// Run through all of the updates we have and re-assign their priority depending
        ///  on what is now going on in the Scene
        /// </summary>
        void Reprioritize();

        /// <summary>
        /// Reset all lists that have to deal with what updates the viewer has
        /// </summary>
        void Reset();
    }

    public interface IPrioritizer
    {
        double GetUpdatePriority (IClientAPI client, ISceneEntity entity);
        double RootReprioritizationDistance { get; }
        double ChildReprioritizationDistance { get; }
    }

    public interface IAnimator
    {
        AnimationSet Animations { get; }
        string CurrentMovementAnimation { get; }
        void UpdateMovementAnimations ();
        void AddAnimation (UUID animID, UUID objectID);
        bool AddAnimation (string name, UUID objectID);
        void RemoveAnimation (UUID animID);
        bool RemoveAnimation (string name);
        void ResetAnimations ();
        void TrySetMovementAnimation (string anim);
        UUID[] GetAnimationArray ();
        void SendAnimPack (UUID[] animations, int[] seqs, UUID[] objectIDs);
        void SendAnimPackToClient (IClientAPI client);
        void SendAnimPack ();
        void Close ();
    }
}