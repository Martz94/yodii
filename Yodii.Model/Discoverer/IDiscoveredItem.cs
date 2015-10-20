#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Model\Discoverer\IDiscoveredItem.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

namespace Yodii.Model
{
    /// <summary>
    /// Discovered item.
    /// </summary>
    public interface IDiscoveredItem
    {
        /// <summary>
        /// Gets the assembly info that contains this item.
        /// </summary>
        IAssemblyInfo AssemblyInfo { get; }

        /// <summary>
        /// Whether this item is on error.
        /// Typically an error occurred during the discovery phase.
        /// </summary>
        bool HasError { get; }

        /// <summary>
        /// Gets the error message associated to this item.
        /// Typically an error that happened during the discovery phase.
        /// </summary>
        string ErrorMessage { get; }
    }
}