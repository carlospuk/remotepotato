using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Configuration.Provider;
using System.Collections.Specialized;
using Microsoft.Win32;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using System.Threading;


public class CommonSettingsProvider : SettingsProvider
{
    private XmlDocument settingsXML = null;
    const string SETTINGSROOT = "Settings";     //XML Root Node
    const string APPLICATIONAME = "RemotePotato";   // Common App Name
    // TODO:  Version control...  (see RegistrySettingsProvider)

    public override void  Initialize(string name, NameValueCollection config)
    {
 	     base.Initialize(APPLICATIONAME, config);
    }

    public override string ApplicationName
    {
        get
        {
            return APPLICATIONAME;
        }
        set
        {
            // do nothing
        }
    }

    public string GetAppSettingsPath()
    {
        string settingsPath = Path.Combine(AppDataFolder, "Settings");
        if (!Directory.Exists(settingsPath))
            Directory.CreateDirectory(settingsPath);
        
        return settingsPath;

        //Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
//        System.IO.FileInfo fi = new System.IO.FileInfo( Assembly.GetAssembly(typeof(CommonSettingsProvider)).Location );
  //      return fi.DirectoryName;
        
    }

    // DIRECTLY COPIED FROM FUNCTIONS.CS IN ASSEMBLY RPSERVER - KEEP IN SYNC MANUALLY
    private string AppDataFolder
    {
        get
        {
            string dirPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\" + "RemotePotato";
            if (!Directory.Exists(dirPath))
            {
                try
                {
                    Directory.CreateDirectory(dirPath);
                }
                catch 
                {
                    return "";
                }
            }
            return dirPath;
        }
    }
    public string GetAppSettingsFilename()
    {
        return APPLICATIONAME + ".settings";
    }


    void SyncToDisk()
    {
        settingsXML.Save(System.IO.Path.Combine(GetAppSettingsPath(), GetAppSettingsFilename()));
    }

    object SetValuesLock = new object();
    public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
    {
        Monitor.Enter(SetValuesLock);



        try
        {

            foreach (SettingsPropertyValue propval in collection)
            {
                SetValue(propval);
            }

            SyncToDisk();
        }
        catch
        {
            // ignore
        }
        finally
        {
            Monitor.Exit(SetValuesLock);
        }
    }
    object GetValuesLock = new object();
    public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
    {
        Monitor.Enter(GetValuesLock);
        SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();

        //Iterate through the settings to be retrieved
        foreach(SettingsProperty setting in collection)
        {
            SettingsPropertyValue value = new SettingsPropertyValue(setting);

            value.IsDirty = false;
            value.SerializedValue = GetValue(setting);
            values.Add(value);
        }

        Monitor.Exit(GetValuesLock);
        return values;
    }

    private XmlDocument SettingsXML
    {
        get
        {
            if (settingsXML == null)
            {
                settingsXML = new XmlDocument();
                try
                {
                    settingsXML.Load(System.IO.Path.Combine(GetAppSettingsPath(), GetAppSettingsFilename()));
                }
                catch  
                {
                    XmlDeclaration dec = settingsXML.CreateXmlDeclaration("1.0", "utf-8", String.Empty);
                    settingsXML.AppendChild(dec);
                    XmlNode nodeRoot = settingsXML.CreateNode(XmlNodeType.Element, SETTINGSROOT, "");
                    settingsXML.AppendChild(nodeRoot);
                }
            }
            return settingsXML;
        }

    }

    object GetLock = new object();
    private string GetValue(SettingsProperty setting)
    {
        Monitor.Enter(GetLock);

        string ret = "";

        try
        {
            ret = SettingsXML.SelectSingleNode(SETTINGSROOT + "/" + setting.Name).InnerText;
        }
        catch
        {
            if (setting.DefaultValue != null)
            {
                ret = setting.DefaultValue.ToString();
            }
            else
            {
                ret = "";
            }
        }
        finally
        {
            Monitor.Exit(GetLock);
        }
        return ret;
    }
    object SetLock = new object();
    private void SetValue(SettingsPropertyValue propVal)
    {
        Monitor.Enter(SetLock);

        XmlElement settingNode;

        try
        {
            settingNode = (XmlElement)SettingsXML.SelectSingleNode(SETTINGSROOT + "/" + propVal.Name);
            settingNode.InnerText = propVal.SerializedValue.ToString();
        }
        catch
        {
            settingNode = SettingsXML.CreateElement(propVal.Name);
            settingNode.InnerText = propVal.SerializedValue.ToString();
            SettingsXML.SelectSingleNode(SETTINGSROOT).AppendChild(settingNode);
        }
        finally
        {
            Monitor.Exit(SetLock);
        }
    }


}