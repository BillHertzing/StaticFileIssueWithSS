using System;
using System.Collections;
using System.Collections.Generic;

namespace Agent.GUIServices.Shared
{
    #region GUIMapping
    public interface IGUIMapping
    {
        public string RelativeToContentRootPath { get; set; }
        public string VirtualRootPath { get; set; }
    }
    public class GUIMapping : IGUIMapping
    {
        public string RelativeToContentRootPath { get; set; }
        public string VirtualRootPath { get; set; }
    }

    #endregion
    #region GUI
    public interface IGUI
    {
        public string Handle { get; set; }
        public IGUIMapping GUIMapping { get; set; }
    }
    public class GUI : IGUI
    {
        public string Handle { get; set; }
        public IGUIMapping GUIMapping { get; set; }
    }

    #endregion
    #region GUIS
    public interface IGUIS
    {
        public IEnumerable<GUIMapping> GUIs { get; set; }
    }
    public class GUIS : IGUIS
    {
        public IEnumerable<GUIMapping> GUIs { get; set; }
    }


    #endregion
    #region ConfigurationData
    public class ConfigurationData
    {
        //ToDo: 
        public ConfigurationData() : this(string.Empty, string.Empty) { }

        public ConfigurationData(string identifier, string version)
        {
            Identifier = identifier;
            Version = version;
        }

        public string Identifier { get; set; }
        public string Version { get; set; }
    }
    #endregion

    #region UserData
    public class UserData
    {
        public UserData() : this(string.Empty) { }
        public UserData(string placeholder)
        {
            Placeholder = placeholder;
        }

        public string Placeholder { get; set; }
    }
    #endregion UserData
}