﻿// ****************************************************************************
// This file belongs to the Chaos-Server project.
// 
// This project is free and open-source, provided that any alterations or
// modifications to any portions of this project adhere to the
// Affero General Public License (Version 3).
// 
// A copy of the AGPLv3 can be found in the project directory.
// You may also find a copy at <https://www.gnu.org/licenses/agpl-3.0.html>
// ****************************************************************************

namespace Chaos
{
    internal struct DialogMenuItem
    {
        internal ushort DialogId { get; }
        internal string Text { get; }
        internal PursuitIds PursuitId { get; }

        /// <summary>
        /// Master constructor for a structure representing an item for a dialog menu.
        /// </summary>
        internal DialogMenuItem(ushort dialogId, string text, PursuitIds pursuitId = PursuitIds.None)
        {
            DialogId = dialogId;
            Text = text;
            PursuitId = pursuitId;
        }
    }
}
