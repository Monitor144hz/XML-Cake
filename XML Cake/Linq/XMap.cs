﻿namespace XmlCake.Linq; 

using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Xml.Linq;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Media.Animation;

public class XMap : XDocument
{
    // private XElement Root { get; set; }

    public enum DuplicateKeyPolicy
    {
        None = 0,
        Ignore = 1,
        Reset = 2,
        RemoveOld = 3,
        RemoveNew = 4
    }


    public Dictionary<string, XElement> mappedElements { get; private set; } = new Dictionary<string, XElement>();
    public char XPATH_DIVIDER { get; private set; } = '/';

    public Func<XElement, string> GenerateKey;

	private Action<string, XElement> MapElementToPath;

	public XMap(XDocument document, Func<XElement, string> generateKey) : base(document)
    {
        GenerateKey = generateKey;
        MapElementToPath = MapNormal; 
    }

    public XMap(XDocument document) : base(document)
    {
        GenerateKey = DefaultGenerateKey;
        MapElementToPath = MapNormal; 
	}

    public static new XMap Load(string uri) => new XMap(XDocument.Load(uri)); 

    public static string TryGetAttributeName(string attributeName, XElement element)
    {
		XAttribute? attribute = element.Attribute(attributeName);
		return attribute is not null ? attribute.Value : element.NodeType.ToString();
	}

    public string DefaultGenerateKey(XElement element)
    {
        XAttribute? attribute = element.Attribute("name"); 
        return attribute is not null ? attribute.Value : element.NodeType.ToString(); 
    }

    public XElement Lookup(string path) => mappedElements[path];

    public bool PathExists(string path) => mappedElements.ContainsKey(path);
	/// <summary>
	/// Return element at target path, with the given root and key generate function. Near to O(1) time complexity when path is mapped, O(n) when unmapped.
	/// </summary>
	/// <param name="path"></param>
	/// <param name="workingRoot"></param>
	/// <param name="generateKey"></param>
	/// <returns></returns>
	public XElement NavigateTo(string path, XElement workingRoot, Func<XElement, string> generateKey)
	{
		XElement element;
 
		if (mappedElements.TryGetValue(path, out element!)) return element;

		string[] pathSections = path.Split('/');
		element = workingRoot;
 
		foreach (string section in pathSections)
		{
			element = FindChildByKey(element, section, generateKey);
		}
		return element;
	}

	/// <summary>
	/// Return element at target path, using document root.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public XElement NavigateTo(string path) => NavigateTo(path, Root!);

	/// <summary>
	/// Return element at target path, with the given root. Near to O(1) time complexity when path is mapped, O(n) when unmapped.
	/// </summary>
	/// <param name="path"></param>
	/// <param name="workingRoot"></param>
	/// <returns></returns>
	public XElement NavigateTo(string path, XElement workingRoot) => NavigateTo(path, workingRoot, GenerateKey);

    
	public void MapLayer(bool ignoreParents, params int[] pathIndices)
    {
        List<XElement> layerElements = new List<XElement>() { Root! };
        XElement pathElement;
        StringBuilder parentPathBuilder = new StringBuilder();
        for (int d = 0; d < pathIndices.Length; d++)
        {
            pathElement = layerElements[pathIndices[d]];
            if (!ignoreParents) MapChildElement(parentPathBuilder.ToString(), pathElement, 0);
            layerElements = pathElement.Elements().ToList();
        }

        string parentPath = parentPathBuilder.ToString();
        for (int i = 0; i < layerElements.Count; i++)
        {
            XElement element = layerElements[i];
            MapChildElement(parentPath, element, i);
        }
    }

	public void MapLayer(XElement workingRoot, bool useBlankPath)
	{



		string rootPath = useBlankPath ? string.Empty : GetPath(workingRoot);
		List<XElement> layerElements = workingRoot.Elements().ToList();
		Debug.Assert(layerElements.Count > 0);
		for (int i = 0; i < layerElements.Count; i++)
		{

			XElement element = layerElements[i];
			MapChildElement(rootPath, element, i);
		}
	}
	/// <summary>
	/// Maps all immediate children of the target element. 
	/// </summary>
	/// <param name="rootPath"></param>
	/// <param name="useBlankPath">whether mapped children use a blank root path</param>
	public void MapLayer(string rootPath, bool useBlankPath)
    {

        XElement workingRoot = NavigateTo(rootPath);
        
        rootPath = useBlankPath ? string.Empty : rootPath; 
        List<XElement> layerElements = workingRoot.Elements().ToList();
        Debug.Assert(layerElements.Count > 0); 
        for (int i = 0; i < layerElements.Count; i++)
        {
            
            XElement element = layerElements[i];
            MapChildElement(rootPath, element, i);
        }
    }
	/// <summary>
	/// Maps all immediate children of the target element. 
	/// </summary>
	/// <param name="rootPath"></param>
	public void MapLayer(string rootPath) => MapLayer(rootPath, true);

    public void MapSlice(XElement workingRoot, int depth, bool useBlankPath)
    {
		if (depth < 1) return;
		string rootPath = useBlankPath ? string.Empty : GetPath(workingRoot);
		List<XElement> layerElements = workingRoot.Elements().ToList();
		depth -= 1;
		for (int i = 0; i < layerElements.Count; i++)
		{
			XElement element = layerElements[i];
			string currentPath = MapChildElement(rootPath, element, i);
			MapSlice(currentPath, depth, useBlankPath);
		}
	}

    public void MapSlice(XElement workingRoot, bool useBlankPath)
	{
		string rootPath = useBlankPath ? string.Empty : GetPath(workingRoot);
		List<XElement> layerElements = workingRoot.Elements().ToList();
		for (int i = 0; i < layerElements.Count; i++)
		{
			XElement element = layerElements[i];
			string currentPath = MapChildElement(rootPath, element, i);
			MapSlice(currentPath, false);
		}
	}


	/// <summary>
	/// Map all children to a specified generation depth.
	/// </summary>
	/// <param name="rootPath"></param>
	/// <param name="depth"></param>
	/// <param name="useBlankPath"></param>
	public void MapSlice(string rootPath, int depth, bool useBlankPath) //vertical mapping 
    {
        if (depth < 1) return;
        XElement workingRoot = NavigateTo(rootPath);
        rootPath = useBlankPath ? string.Empty : rootPath; 
        List<XElement> layerElements = workingRoot.Elements().ToList();
        depth -= 1; 
        for (int i = 0; i < layerElements.Count; i++)
        {
            XElement element = layerElements[i];
            string currentPath = MapChildElement(rootPath, element, i);
            MapSlice(currentPath, depth, useBlankPath);
        }
        
    }
    /// <summary>
    /// Map all children.
    /// </summary>
    /// <param name="rootPath"></param>
    /// <param name="useBlankPath"></param>
    public void MapSlice(string rootPath, bool useBlankPath)
    {

        XElement workingRoot = !String.IsNullOrEmpty(rootPath) ? NavigateTo(rootPath) : Root!;
        rootPath = useBlankPath ? string.Empty : rootPath; 
        List<XElement> layerElements = workingRoot.Elements().ToList();
        for (int i = 0; i < layerElements.Count; i++)
        {
            XElement element = layerElements[i];
            string currentPath = MapChildElement(rootPath, element, i);
            MapSlice(currentPath, false);
        }
    }


    public void MapSlice(string rootPath) => MapSlice(rootPath, false);

    public void MapSlice(XElement rootElement) => MapSlice(GenerateKey(rootElement), false);

    public void MapAll() => MapSlice(string.Empty, false); 

    public void ReplaceElement(string path, XElement newElement)
    {
        
        XElement targetElement = NavigateTo(path); 
        
        Debug.Assert(targetElement is not null, $"Target element at path:{path} does not exist."); 
        Debug.Assert(targetElement.Parent is not null, $"Target element at path:{path} has no parent.");
        lock(targetElement.Parent)
        lock(targetElement)
        {
            targetElement.ReplaceWith(newElement);
            if (targetElement == newElement) MapResetDuplicates(path, newElement);
        }
    }

    public void AppendElement(string path, XElement element)
    {
        XElement targetElement = NavigateTo(path); 

        Debug.Assert(targetElement is not null, $"Target element at path:{path} does not exist."); 
        lock(targetElement)
        {
            targetElement.Add(element); 
        }
    }

    public XElement CopyElement(string path)
    {
        XElement targetElement = NavigateTo(path); 
        Debug.Assert(targetElement is not null, $"Target element at path:{path} does not exist.");
        return new XElement(targetElement); 
    }

    
    private string GetDefaultPath(string path, string defaultValue, int elementIndex)
    {
        bool IsOpenPath = (path != string.Empty && path[path.Length - 1] != XPATH_DIVIDER);
        StringBuilder defaultBuilder = new StringBuilder(path);

        if (IsOpenPath) defaultBuilder.Append(XPATH_DIVIDER);
        defaultBuilder.Append(defaultValue).Append(elementIndex);

        return defaultBuilder.ToString();
    }

    private string GetUniquePath(string path, string key)
    {
        bool IsOpenPath = (path != string.Empty && path[path.Length - 1] != XPATH_DIVIDER);
        StringBuilder uniqueBuilder = new StringBuilder(path);

        if (IsOpenPath) uniqueBuilder.Append(XPATH_DIVIDER);
        uniqueBuilder.Append(key);

        return uniqueBuilder.ToString();
    }


    private XElement FindChildByKey(XElement parent, string key, Func<XElement, string> generateKey)
    {
		int numIndex;
		if ((numIndex = key.IndexOf(parent.NodeType.ToString())) > -1)
		{
			int childNum = (Int32)key[numIndex + 1];
			return parent.Elements().ElementAt(childNum);
		}
		return parent.Elements().Where(e => generateKey(e) == key).FirstOrDefault(new XElement("null"));
	}

    

    private XElement FindChildByKey(XElement parent, string key) => FindChildByKey(parent, key, GenerateKey);


    private string GetPath(string path, XElement element, int elementIndex)
    {
		string defaultValue = element.NodeType.ToString();
		string key = GenerateKey(element);
		key = (key == defaultValue) ? GetDefaultPath(path, defaultValue, elementIndex) : GetUniquePath(path, key);
        return key; 
	}
    public string GetPath(XElement element) => GetPath(String.Empty, element, 0);
	private string MapChildElement(string path, XElement element, int elementIndex)
    {
        string key = GetPath(path, element, elementIndex);
        lock(mappedElements)
        {
			MapElementToPath(key, element);
		}
        return key; 
    }

    private void MapNormal(string key, XElement element)
    {
		mappedElements.Add(key, element);
	}
	private void MapIgnoreDuplicates(string key, XElement element)
	{
		if (!mappedElements.ContainsKey(key))
		{
			mappedElements.Add(key, element);
		}
	}
	private void MapResetDuplicates(string key, XElement element)
	{
		if (mappedElements.ContainsKey(key))
		{
			mappedElements[key] = element;
		}
		else
		{
			mappedElements.Add(key, element);
		}
	}
	private void MapRemoveDuplicateOriginal(string key, XElement element)
	{
		XElement? existingElement = null;
		if (mappedElements.TryGetValue(key, out existingElement))
		{
			existingElement.Remove();
			mappedElements[key] = element;
		}
		else
		{
			mappedElements.Add(key, element);
		}
	}
	private void MapRemoveDuplicateNew(string key, XElement element)
	{
		XElement? existingElement = null;
		if (mappedElements.TryGetValue(key, out existingElement))
		{
			element.Remove();
		}
		else
		{
			mappedElements.Add(key, element);
		}
	}
}