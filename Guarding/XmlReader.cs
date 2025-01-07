using GTA.Math;
using System.Collections.Generic;
using System.Xml.Linq;

public class XmlReader
{
    private readonly string _xmlFilePath; // XML File Path

    public XmlReader(string filePath)
    {
        _xmlFilePath = filePath; // Set file path
    }

    public List<Area> LoadAreasFromXml() // Load areas from XML
    {
        var areas = new List<Area>(); // Store areas

        XElement xml = XElement.Load(_xmlFilePath); // Load XML

        foreach (var areaElement in xml.Elements("Area")) // Iterate through areas
        {
            string areaName = areaElement.Attribute("name")?.Value; // Get area name
            string model = areaElement.Attribute("model")?.Value; // Get area model name
            Area area = new Area(areaName, model); // Create new area with model

            foreach (var spawnPointElement in areaElement.Elements("SpawnPoint")) // Iterate spawn points
            {
                var positionElement = spawnPointElement.Element("Position"); // Get position
                float x = float.Parse(positionElement.Attribute("x")?.Value); // X position
                float y = float.Parse(positionElement.Attribute("y")?.Value); // Y position
                float z = float.Parse(positionElement.Attribute("z")?.Value); // Z position

                float heading = float.Parse(spawnPointElement.Element("Heading")?.Value); // Heading

                Vector3 position = new Vector3(x, y, z); // Create position vector
                area.AddSpawnPoint(position, heading); // Add spawn point to area
            }

            areas.Add(area); // Add area to list
        }

        return areas; // Return list of areas
    }
}
