using Autodesk.DataExchange.Core.Enums;
using Autodesk.DataExchange.DataModels;
using Autodesk.DataExchange.SchemaObjects.Units;
using Autodesk.GeometryPrimitives.Data;
using Autodesk.GeometryPrimitives.Data.DX;
using Autodesk.GeometryUtilities.MeshAPI;
using Autodesk.Parameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SampleConnector
{
    public class CreateExchangeHelper
    {
        private RenderStyle commonRenderStyle = new RenderStyle("Common Render Style", new RGBA(255, 0, 0, 255), 1);

        private static readonly string BrepGeometryFilePath = GetSampleFilePath("nist_ftc_09_asme1_rd.stp");
        private static readonly string MeshGeometryFilePath = GetSampleFilePath("ball.obj");
        private static readonly string IfcGeometryFilePath = GetSampleFilePath("BasinAdvancedBrep.ifc");
        private static readonly string BuiltInParamsFilePath = GetSampleFilePath("BuiltInParams.json");


        /// <summary>
        /// Common render style used across the application.
        /// </summary>
        internal static readonly RenderStyle CommonRenderStyle = new RenderStyle(
            "Sample Render Style",
            new RGBA(255, 0, 0, 255),
            1);


        private static readonly Autodesk.GeometryUtilities.MeshAPI.Mesh SampleMeshApiObject = new Autodesk.GeometryUtilities.MeshAPI.Mesh
        {
            MeshColor = new Color(0.5f, 0.0f, 0.70f, 1.0f),
            Vertices = new List<Vertex>
            {
                new Vertex(-1, -1, -1),
                new Vertex(1, -1, -1),
                new Vertex(1, 1, -1),
                new Vertex(-1, 1, -1),
                new Vertex(-1, -1, 1),
                new Vertex(1, -1, 1),
                new Vertex(1, 1, 1),
                new Vertex(-1, 1, 1)
},
            Faces = new List<Face>
            {
                new Face { Corners = new List<int> { 0, 2, 1 }, FaceColor = new Color(1.0f, 1.0f, 1.0f, 1.0f) },
                new Face { Corners = new List<int> { 0, 3, 2 }, FaceColor = new Color(1.0f, 1.0f, 1.0f, 1.0f) },
                new Face { Corners = new List<int> { 4, 5, 6 }, FaceColor = new Color(1.0f, 1.0f, 1.0f, 1.0f) },
                new Face { Corners = new List<int> { 4, 6, 7 }, FaceColor = new Color(0.0f, 0.4f, 1.0f, 1.0f) },
                new Face { Corners = new List<int> { 0, 3, 7 }, FaceColor = new Color(1.0f, 1.0f, 1.0f, 1.0f) },
                new Face { Corners = new List<int> { 0, 7, 4 }, FaceColor = new Color(1.0f, 1.0f, 1.0f, 1.0f) },
                new Face { Corners = new List<int> { 1, 5, 6 }, FaceColor = new Color(1.0f, 1.0f, 1.0f, 1.0f) },
                new Face { Corners = new List<int> { 1, 6, 2 }, FaceColor = new Color(1.0f, 0.3f, 1.0f, 1.0f) },
                new Face { Corners = new List<int> { 3, 2, 7 }, FaceColor = new Color(1.0f, 1.0f, 1.0f, 1.0f) },
                new Face { Corners = new List<int> { 2, 6, 7 }, FaceColor = new Color(1.0f, 1.0f, 0.1f, 1.0f) },
                new Face { Corners = new List<int> { 0, 1, 5 }, FaceColor = new Color(1.0f, 1.0f, 1.0f, 1.0f) },
                new Face { Corners = new List<int> { 0, 5, 4 }, FaceColor = new Color(1.0f, 1.0f, 0.8f, 1.0f) }

            }
        };

        /// <summary>
        /// Adds objects with varied geometry types cycling through STP, IFC, OBJ, and MeshAPI.
        /// </summary>
        public static Task AddVariedGeometryObjects(ElementDataModel dataModel, int numberOfObjects)
        {
            if (dataModel == null) throw new ArgumentNullException(nameof(dataModel));

            try
            {
                string uniquePrefix = GetRandomId();
                for (int i = 0; i < numberOfObjects; i++)
                {
                    var element = dataModel.AddElement(CreateElementProperties(
                        $"Object-{uniquePrefix}-{i + 1}",
                        $"Object-{uniquePrefix}-{i + 1}"));

                    var geometry = CreateGeometryByType(i % 4, i);
                    dataModel.SetElementGeometry(element, new List<ElementGeometry> { geometry });
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding varied geometry objects: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generates a random unique identifier for naming purposes.
        /// </summary>
        public static string GetRandomId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 5);
        }

        private static ElementProperties CreateElementProperties(string id, string name)
        {
            return new ElementProperties(id, name, "Generic", "Generic", "Generic Object");
        }

        /// <summary>
        /// Adds a unique string parameter to the specified element.
        /// </summary>
        public static async Task AddUniqueStringParameter(Element element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            var uniqueId = GetRandomId();
            await AddStringParameter(element, uniqueId);
        }

        private static async Task AddStringParameter(Element element, string uniqueId)
        {
            var parameter = new Parameter($"TestString{uniqueId}", "TestStringValue")
            {
                SampleText = "Sample string parameter",
                Description = "Demo string parameter for sample connector",
                ReadOnly = false,
                IsCustomParameter = true,
                GroupID = Group.Graphics.DisplayName()
            };

            await element.CreateInstanceParameterAsync(parameter);
        }

        //TODO: question: do we need to create geometry using files?
        private static ElementGeometry CreateGeometryByType(int geometryType, int index)
        {
            switch (geometryType)
            {
                case 0:
                    return ElementDataModel.CreateFileGeometry(new GeometryProperties(BrepGeometryFilePath, CommonRenderStyle));
                case 1:
                    return ElementDataModel.CreateFileGeometry(new GeometryProperties(IfcGeometryFilePath, CommonRenderStyle));
                case 2:
                    return ElementDataModel.CreateFileGeometry(new GeometryProperties(MeshGeometryFilePath, CommonRenderStyle));
                case 3:
                    return ElementDataModel.CreateMeshGeometry(new GeometryProperties(SampleMeshApiObject, $"MeshAPI-{index}"));
                default:
                    throw new ArgumentOutOfRangeException(nameof(geometryType), "Invalid geometry type");
            }
        }


        public void AddPrimitiveGeometries(ElementDataModel data)
        {
            AddPrimitiveLineGeometries(data);
            AddPrimitivePointGeometry(data);
            AddPrimitiveCurveAndSurfaceGeometries(data);
            AddPrimitivePolylineGeometry(data);
        }

        private static string GetSampleFilePath(string fileName)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                               "SampleInputFiles", fileName);
        }

        private void AddPrimitiveLineGeometries(ElementDataModel data)
        {
            var newElement = data.AddElement(new ElementProperties("Line1", "SampleLine", "Generics", "Generic", "Generic Object"));

            var newBRepElementGeometry = new List<ElementGeometry>();

            CurveSet setOfLines = new CurveSet();

            Line lineone = new Line(new Point3d { X = 200, Y = 200, Z = 200 }, new Vector3d { X = 100, Y = 400, Z = 300 });
            ParamRange range = new ParamRange
            {
                High = 7.25,
                Low = 0,
                Type = ParamRange.RangeType.Finite
            };
            lineone.Range = range;
            setOfLines.Add(lineone);

            newBRepElementGeometry.Add(ElementDataModel.CreatePrimitiveGeometry(new GeometryProperties(setOfLines, commonRenderStyle)));
            data.SetElementGeometry(newElement, newBRepElementGeometry);

            var newLineElement2 = data.AddElement(new ElementProperties("Line2", "SampleLine", "Generics", "Generic", "Generic Object"));

            var newlineElementGeometry = new List<ElementGeometry>();

            CurveSet settwoOfLines = new CurveSet();
            Line linetwo = new Line(new Point3d { X = -53.34, Y = 10.16, Z = 220.98 }, new Vector3d { X = 0, Y = 0, Z = -30.48 });

            linetwo.Range = range;
            settwoOfLines.Add(linetwo);

            CurveSet setthreeOfLines = new CurveSet();
            Line linethree = new Line(new Point3d { X = -53.34, Y = 10.16, Z = 220.98 }, new Vector3d { X = 30.48, Y = 5.7, Z = 0 });

            linethree.Range = range;
            setthreeOfLines.Add(linethree);

            newlineElementGeometry.Add(ElementDataModel.CreatePrimitiveGeometry(new GeometryProperties(settwoOfLines, commonRenderStyle)));
            newlineElementGeometry.Add(ElementDataModel.CreatePrimitiveGeometry(new GeometryProperties(setthreeOfLines, commonRenderStyle)));
            data.SetElementGeometry(newLineElement2, newlineElementGeometry);

        }

        public void AddNISTObject(ElementDataModel data, Element newBRep)
        {
            var newBRepGeometry = new List<ElementGeometry>();
            var filePath = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\InputStepFile\\cone.stp";
            newBRepGeometry.Add(ElementDataModel.CreateFileGeometry(new GeometryProperties(filePath, commonRenderStyle)));
            data.SetElementGeometry(newBRep, newBRepGeometry);
        }

        public void AddPrimitivePointGeometry(ElementDataModel data)
        {
            //....Primitive geometry - One Point...
            var newPointElement = data.AddElement(new ElementProperties("Point1", "SamplePoint", "Generics", "Generic", "Point"));
            var newPointElementGeometry = new List<ElementGeometry>();
            DesignPoint point = new DesignPoint(10.0, 10.0, 10.0);
            newPointElementGeometry.Add(ElementDataModel.CreatePrimitiveGeometry(new GeometryProperties(point, commonRenderStyle)));
            data.SetElementGeometry(newPointElement, newPointElementGeometry);

        }
        public void AddPrimitiveCurveAndSurfaceGeometries(ElementDataModel data)
        {
            var circleElement = data.AddElement(new ElementProperties("Circle", "SampleCircle", "CircleGenerics", "CircleGeneric", "CircleElement"));
            var circleElementGeometry = new List<ElementGeometry>();
            var geomContainer = new GeometryContainer();

            AddCurveGeometries(geomContainer);
            AddSurfaceGeometries(geomContainer);

            circleElementGeometry.Add(ElementDataModel.CreatePrimitiveGeometry(new GeometryProperties(geomContainer, commonRenderStyle)));
            data.SetElementGeometry(circleElement, circleElementGeometry);
        }

        private void AddCurveGeometries(GeometryContainer geometryContainer)
        {
            geometryContainer.Curves = new List<Curve>();

            AddCircleGeometries(geometryContainer);

            //Add BCurve
            geometryContainer.Curves.Add(GetBCurveGeometry());

            //Add Ellipse Geometry
            geometryContainer.Curves.Add(GetEllipseGeometry());
        }

        private void AddCircleGeometries(GeometryContainer geometryContainer)
        {
            geometryContainer.Curves.Add(new Circle()
            {
                Center = new Point3d(0, 0, 0),
                Normal = new Vector3d(0, 0, 1),
                Radius = new Vector3d(500, 0, 0)
            });

            //CCW 90degree
            geometryContainer.Curves.Add(new Circle()
            {
                Center = new Point3d(700, 0, 0),
                Normal = new Vector3d(0, 0, 1),
                Radius = new Vector3d(200, 0, 0),
                Range = new ParamRange(ParamRange.RangeType.Finite, 0, 1.5708)
            });

            //CW 90degree
            geometryContainer.Curves.Add(new Circle()
            {
                Center = new Point3d(1000, 0, 0),
                Normal = new Vector3d(0, 0, 1),
                Radius = new Vector3d(200, 0, 0),
                Range = new ParamRange(ParamRange.RangeType.Finite, -1.5708, 0)
            });
        }

        private BCurve GetBCurveGeometry()
        {
            return new BCurve()
            {
                Degree = 3,
                Knots = new List<double>() {
                        0, 0, 0, 0,
                        22.052499319464456,
                        39.56011633518649,
                        61.767382623682536,
                        86.37111048613733, 86.37111048613733, 86.37111048613733, 86.37111048613733
                    },
                ControlPoints = new List<Point3d>()
                {
                    new Point3d(-2117.6323100866352, -578.0819231498238, 0),
                    new Point3d(-1412.0457600104123, -249.06151135811626, 0),
                    new Point3d(-1846.6021471151125, 185.49487574658576, 0),
                    new Point3d(-1223.20576487363, 185.49487574658374, 0),
                    new Point3d(-908.8865372007543, 366.96716645499356, 0),
                    new Point3d(-981.7326274133812, -674.7804577820922, 0),
                    new Point3d(-218.14218928271805, -1030.8485267763535, 0)
                },
                Weights = new List<double>() { 1, 1, 1, 1, 1, 1, 1 }
            };
        }

        private Ellipse GetEllipseGeometry()
        {
            return new Ellipse()
            {
                Center = new Point3d(1300, 0, 0),
                Normal = new Vector3d(0, 0, 1),
                MajorRadius = new Vector3d(500, 0, 0),
                RadiusRatio = 0.7
            };
        }

        private void AddSurfaceGeometries(GeometryContainer geometryContainer)
        {
            geometryContainer.Surfaces = new List<Surface>()
            {
                new Plane()
                {
                    Origin = new Point3d(0, -270.888, 7.8900e-3),
                    Normal = new Vector3d(1, 0, 0),
                    UAxis = new Vector3d(0, 1, 0),
                    URange = new ParamRange(
                        ParamRange.RangeType.Finite,
                        0,
                        2000
                    ),
                    VRange = new ParamRange(
                        ParamRange.RangeType.Finite,
                        0,
                        1000
                    )
                },
                new Plane()
                {
                    Origin = new Point3d(0, -2000, 0),
                    Normal = new Vector3d(1, 0, 0),
                    UAxis = new Vector3d(0, 1, 0),
                    URange = new ParamRange(
                        ParamRange.RangeType.Finite,
                        0,
                        700
                    ),
                }
            };
        }

        public void AddMeshGeometry(ElementDataModel data)
        {
            Autodesk.GeometryUtilities.MeshAPI.Mesh inMemoryMesh = new Autodesk.GeometryUtilities.MeshAPI.Mesh()
            {
                Vertices = new List<Vertex>
                        {
                            new Vertex(0.0, 0.0, 0.0),
                            new Vertex(1.0, 0.0, 0.0),
                            new Vertex(0.0, 1.0, 0.0),
                            new Vertex(1.0, 1.0, 0.0),
                        },
                Faces = new List<Face>
                        {
                            new Face()
                            {
                                Corners = new List<int> { 0, 1, 2 },
                                Normals = new List<Normal>
                                {
                                    new Normal(0, 0, 1),
                                    new Normal(0, 0, 1),
                                    new Normal(0, 0, 1),
                                },
                            },
                            new Face()
                            {
                                Corners = new List<int> { 2, 1, 3 },
                                Normals = new List<Normal>
                                {
                                    new Normal(0, 0, 1),
                                    new Normal(0, 0, 1),
                                    new Normal(0, 0, 1),
                                },
                            },
                        },
            };

            var meshObjWithColor = new Autodesk.GeometryUtilities.MeshAPI.Mesh()
            {
                MeshColor = new Color(0.9f, 0.2f, 0.2f, 1.0f),  // mesh body color
                Vertices = new List<Vertex>
                        {
                            new Vertex(0.0, 0.0, 0.0),
                            new Vertex(1.0, 0.0, 0.0),
                            new Vertex(0.0, 1.0, 0.0),
                            new Vertex(1.0, 1.0, 0.0),
                        },
                Faces = new List<Face>
                        {
                            new Face()
                            {
                                Corners = new List<int> { 0, 1, 2 },
                                Normals = new List<Normal>
                                {
                                    new Normal(0, 0, 1),
                                    new Normal(0, 0, 1),
                                    new Normal(0, 0, 1),
                                },
                                FaceColor = new Color(0.2f, 0.2f, 0.9f, 1.0f),  // face color
                            },
                            new Face()
                            {
                                Corners = new List<int> { 2, 1, 3 },
                                Normals = new List<Normal>
                                {
                                    new Normal(0, 0, 1),
                                    new Normal(0, 0, 1),
                                    new Normal(0, 0, 1),
                                },
                                FaceColor = new Color(0.2f, 0.9f, 0.2f, 1.0f),  // face color
                            },
                        },
            };

            var complexMesh = new Autodesk.GeometryUtilities.MeshAPI.Mesh()
            {
                MeshColor = new Color(0.5f, 0.5f, 0.5f, 1.0f),  // mesh body color
                Vertices = new List<Vertex>
                {
                    new Vertex(0.0, 0.0, 0.0),
                    new Vertex(1.0, 0.0, 0.0),
                    new Vertex(0.0, 1.0, 0.0),
                    new Vertex(1.0, 1.0, 0.0),
                    new Vertex(0.0, 0.0, 1.0),
                    new Vertex(1.0, 0.0, 1.0),
                    new Vertex(0.0, 1.0, 1.0),
                    new Vertex(1.0, 1.0, 1.0),
                    new Vertex(0.5, 0.5, 1.5),
                },
                Faces = new List<Face>
                {
                    new Face()
                    {
                        Corners = new List<int> { 0, 1, 2 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                        },
                        FaceColor = new Color(0.2f, 0.2f, 0.9f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 2, 1, 3 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                        },
                        FaceColor = new Color(0.2f, 0.9f, 0.2f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 0, 1, 4 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, 1, 0),
                            new Normal(0, 1, 0),
                            new Normal(0, 1, 0),
                        },
                        FaceColor = new Color(0.9f, 0.2f, 0.2f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 1, 5, 4 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, 1, 0),
                            new Normal(0, 1, 0),
                            new Normal(0, 1, 0),
                        },
                        FaceColor = new Color(0.9f, 0.2f, 0.2f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 0, 2, 4 },
                        Normals = new List<Normal>
                        {
                            new Normal(1, 0, 0),
                            new Normal(1, 0, 0),
                            new Normal(1, 0, 0),
                        },
                        FaceColor = new Color(0.2f, 0.9f, 0.9f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 2, 6, 4 },
                        Normals = new List<Normal>
                        {
                            new Normal(1, 0, 0),
                            new Normal(1, 0, 0),
                            new Normal(1, 0, 0),
                        },
                        FaceColor = new Color(0.2f, 0.9f, 0.9f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 1, 3, 5 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, -1, 0),
                            new Normal(0, -1, 0),
                            new Normal(0, -1, 0),
                        },
                        FaceColor = new Color(0.9f, 0.9f, 0.2f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 3, 7, 5 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, -1, 0),
                            new Normal(0, -1, 0),
                            new Normal(0, -1, 0),
                        },
                        FaceColor = new Color(0.9f, 0.9f, 0.2f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 2, 3, 6 },
                        Normals = new List<Normal>
                        {
                            new Normal(-1, 0, 0),
                            new Normal(-1, 0, 0),
                            new Normal(-1, 0, 0),
                        },
                        FaceColor = new Color(0.9f, 0.2f, 0.9f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 3, 7, 6 },
                        Normals = new List<Normal>
                        {
                            new Normal(-1, 0, 0),
                            new Normal(-1, 0, 0),
                            new Normal(-1, 0, 0),
                        },
                        FaceColor = new Color(0.9f, 0.2f, 0.9f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 4, 5, 6 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, 0, -1),
                            new Normal(0, 0, -1),
                            new Normal(0, 0, -1),
                        },
                        FaceColor = new Color(0.2f, 0.2f, 0.2f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 5, 7, 6 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, 0, -1),
                            new Normal(0, 0, -1),
                            new Normal(0, 0, -1),
                        },
                        FaceColor = new Color(0.2f, 0.2f, 0.2f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 4, 6, 8 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                        },
                        FaceColor = new Color(0.5f, 0.5f, 0.5f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 5, 7, 8 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                        },
                        FaceColor = new Color(0.5f, 0.5f, 0.5f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 6, 7, 8 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                        },
                        FaceColor = new Color(0.5f, 0.5f, 0.5f, 1.0f),  // face color
                    },
                },
            };

            var meshGeom = ElementDataModel.CreateMeshGeometry(new GeometryProperties(meshObjWithColor, "Mesh With Color"));
            var meshElement = data.AddElement(new ElementProperties("Mesh1", "SampleMesh", "Mesh", "Mesh", "In memory mesh"));
            data.SetElementGeometry(meshElement, new List<ElementGeometry> { meshGeom });

            var complexMeshGeom = ElementDataModel.CreateMeshGeometry(new GeometryProperties(complexMesh, "Complex Mesh With Color"));
            var complexMeshElement = data.AddElement(new ElementProperties("ComplexMesh", "ComplexSampleMesh", "Mesh", "Mesh", "Complex In memory mesh"));
            data.SetElementGeometry(complexMeshElement, new List<ElementGeometry> { complexMeshGeom });
        }

        public void AddElementsForExchangeUpdate(ElementDataModel data)
        {
            //Add Element with BRep Geometry
            var newBRepGeometry = new List<ElementGeometry>();
            var filePath = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\InputStepFile\\nist_ftc_09_asme1_rd.stp";
            newBRepGeometry.Add(ElementDataModel.CreateFileGeometry(new GeometryProperties(filePath, commonRenderStyle)));
            var newBRep = data.AddElement(new ElementProperties("0-new", "SampleBrep", "Generics", "Generic", "Non-Generic Object"));
            data.SetElementGeometry(newBRep, newBRepGeometry);

            //Add Element with Mesh Geometry

            var meshObjWithColor = new Autodesk.GeometryUtilities.MeshAPI.Mesh()
            {
                MeshColor = new Color(0.9f, 0.9f, 0.9f, 1.0f),  // mesh body color
                Vertices = new List<Vertex>
                {
                    new Vertex(0.0, 0.0, 0.0),
                    new Vertex(1.0, 0.0, 0.0),
                    new Vertex(0.0, 1.0, 0.0),
                    new Vertex(1.0, 1.0, 0.0),
                },
                Faces = new List<Face>
                {
                    new Face()
                    {
                        Corners = new List<int> { 0, 1, 2 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                        },
                        FaceColor = new Color(0.2f, 0.2f, 0.9f, 1.0f),  // face color
                    },
                    new Face()
                    {
                        Corners = new List<int> { 2, 1, 3 },
                        Normals = new List<Normal>
                        {
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                            new Normal(0, 0, 1),
                        },
                        FaceColor = new Color(0.9f, 0.9f, 0.2f, 1.0f),  // face color
                    },
                },
            };

            var meshGeom = ElementDataModel.CreateMeshGeometry(new GeometryProperties(meshObjWithColor, "Mesh With Color"));
            var meshElement = data.AddElement(new ElementProperties("Mesh3", "SampleMesh", "Mesh", "Mesh", "In memory mesh with Color"));
            data.SetElementGeometry(meshElement, new List<ElementGeometry> { meshGeom });
        }

        private void AddPrimitivePolylineGeometry(ElementDataModel dataModel)
        {
            var polyLineElement = dataModel.AddElement(new ElementProperties("Polyline", "SamplePolyline", "PolylineGenerics", "PolylineGeneric", "PolylineElement"));
            var polyLineElementGeometry = new List<ElementGeometry>();
            var geomContainer = new GeometryContainer()
            {
                Curves = new List<Curve>()
                {
                    new Polyline()
                    {
                        Range = new ParamRange(ParamRange.RangeType.Finite, 0.0, 2.0),
                        Closed = false,
                        Points = new List<Point3d>()
                        {
                            new Point3d(12.5, 4, 0),
                            new Point3d(4.5, 4, 0),
                            new Point3d(11.25, 0, 0)
                        },
                    },
                },
            };

            polyLineElementGeometry.Add(ElementDataModel.CreatePrimitiveGeometry(new GeometryProperties(geomContainer, commonRenderStyle)));
            dataModel.SetElementGeometry(polyLineElement, polyLineElementGeometry);
        }
    }
}
