using Autodesk.DataExchange.Core.Enums;
using Autodesk.DataExchange.DataModels;
using Autodesk.DataExchange.SchemaObjects.Units;
using Autodesk.GeometryPrimitives.Design;
using Autodesk.GeometryPrimitives.Geometry;
using Autodesk.GeometryPrimitives.Math;
using Autodesk.Parameters;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SampleConnector
{
    public class CreateExchangeHelper
    {
        private RenderStyle commonRenderStyle = new RenderStyle("Common Render Style", new RGBA(255, 0, 0, 255), 1);

        public void AddWallGeometry(ElementDataModel data)
        {
            ElementGeometry wallGeometry = ElementDataModel.CreateGeometry(new GeometryProperties($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\InputStepFile\\11DB159F6864D8FC02B33D7E9280498F08DFC4FB.stp", commonRenderStyle));

            var wallElement = data.AddElement(new ElementProperties("1", "Wall-1", "Walls", "Wall", "Generic Wall"));
            var wallGeometries = new List<ElementGeometry> { wallGeometry };

            data.SetElementGeometryByElement(wallElement, wallGeometries);
        }

        public void AddGeometryWithLengthUnit(ElementDataModel data)
        {
            var millimeterRodStepFile = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\InputStepFile\\1000mm_rod.stp";

            //Specify default LengthUnit of the step file; for this file it is millimeters
            var millimeterRodGeometry = ElementDataModel.CreateGeometry(new GeometryProperties(millimeterRodStepFile, commonRenderStyle) { LengthUnit = UnitFactory.MilliMeter, DisplayLengthUnit = UnitFactory.MilliMeter, DisplayAngleUnit = UnitFactory.Radian });

            var rodElement = data.AddElement(new ElementProperties("RodElement", "SampleRod", "GenericRods", "GenericRod", "Generic Rod") { LengthUnit = UnitFactory.MilliMeter, DisplayLengthUnit = UnitFactory.MilliMeter });

            var rodElementGeometry = new List<ElementGeometry>() { millimeterRodGeometry };

            data.SetElementGeometryByElement(rodElement, rodElementGeometry);
        }

        public void AddPrimitiveGeometries(ElementDataModel data)
        {
            AddPrimitiveLineGeometries(data);
            AddPrimitivePointGeometry(data);
            AddPrimitiveCurveAndSurfaceGeometries(data);
            AddPrimitivePolylineGeometry(data);
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
            data.SetElementGeometryByElement(newElement, newBRepElementGeometry);

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
            data.SetElementGeometryByElement(newLineElement2, newlineElementGeometry);

        }

        public void AddNISTObject(ElementDataModel data, Element newBRep)
        {
            var newBRepGeometry = new List<ElementGeometry>();
            var filePath = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\InputStepFile\\cone.stp";
            newBRepGeometry.Add(ElementDataModel.CreateGeometry(new GeometryProperties(filePath, commonRenderStyle)));
            data.SetElementGeometryByElement(newBRep, newBRepGeometry);
        }

        public void AddPrimitivePointGeometry(ElementDataModel data)
        {
            //....Primitive geometry - One Point...
            var newPointElement = data.AddElement(new ElementProperties("Point1", "SamplePoint", "Generics", "Generic", "Point"));
            var newPointElementGeometry = new List<ElementGeometry>();
            DesignPoint point = new DesignPoint(10.0, 10.0, 10.0);
            newPointElementGeometry.Add(ElementDataModel.CreatePrimitiveGeometry(new GeometryProperties(point, commonRenderStyle)));
            data.SetElementGeometryByElement(newPointElement, newPointElementGeometry);

        }
        public void AddPrimitiveCurveAndSurfaceGeometries(ElementDataModel data)
        {
            var circleElement = data.AddElement(new ElementProperties("Circle", "SampleCircle", "CircleGenerics", "CircleGeneric", "CircleElement"));
            var circleElementGeometry = new List<ElementGeometry>();
            var geomContainer = new GeometryContainer();

            AddCurveGeometries(geomContainer);
            AddSurfaceGeometries(geomContainer);

            circleElementGeometry.Add(ElementDataModel.CreatePrimitiveGeometry(new GeometryProperties(geomContainer, commonRenderStyle)));
            data.SetElementGeometryByElement(circleElement, circleElementGeometry);
        }

        private void AddCurveGeometries(Autodesk.GeometryPrimitives.Design.GeometryContainer geometryContainer)
        {
            geometryContainer.Curves = new CurveArray();

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
            geometryContainer.Surfaces = new SurfaceArray()
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
            var newMeshElement = data.AddElement(new ElementProperties("MeshEElement", "SampleMesh", "GenericsMesh", "GenericMesh", "Mesh Object"));
            var newMeshGeometry = new List<ElementGeometry>();
            var filePathMesh = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\InputStepFile\\mesh1.obj";
            newMeshGeometry.Add(ElementDataModel.CreateGeometry(new GeometryProperties(filePathMesh, commonRenderStyle)));
            data.SetElementGeometryByElement(newMeshElement, newMeshGeometry);
        }

        public void AddIFCGeometry(ElementDataModel data)
        {
            var newIfcBrep = data.AddElement(new ElementProperties("NISTIFC", "SampleIFC", "IFCs", "IFC", "IFC Object"));

            var newIfcBRepGeometry = new List<ElementGeometry>();
            var ifcfilePath = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\InputStepFile\\Beam.ifc";
            newIfcBRepGeometry.Add(ElementDataModel.CreateGeometry(new GeometryProperties(ifcfilePath, commonRenderStyle)));
            data.SetElementGeometryByElement(newIfcBrep, newIfcBRepGeometry);
        }

        public async Task AddCustomParametersToElement(ElementDataModel elementDataModel, Element element, string schemaNamespace)
        {
            /* Custom Instance Parameters */
            //create bool Custom parameter for instance
            await CreateCustomInstanceParameter_Bool(element, schemaNamespace);

            //create float Custom parameter for instance
            await CreateCustomInstanceParameter_Float(element, schemaNamespace);

            //create int Custom parameter for instance
            await CreateCustomInstanceParameter_Int(element, schemaNamespace);

            //Create string Custom parameter for instance
            await CreateCustomInstanceParameter_String(element, schemaNamespace);

            /* Custom Design Parameters */
            //Create bool Custom parameter for design
            await CreateCustomDesignParameter_Bool(elementDataModel, schemaNamespace);

            //Create float Custom parameter for design
            await CreateCustomDesignParameter_Float(elementDataModel, schemaNamespace);

            //Create int Custom parameter for design
            await CreateCustomDesignParameter_Int(elementDataModel, schemaNamespace);

            //Create string Custom parameter for design
            await CreateCustomDesignParameter_String(elementDataModel, schemaNamespace);
        }

        private async Task CreateCustomInstanceParameter_Bool(Element element, string schemaNamespace)
        {
            string schemaId = "exchange.parameter." + schemaNamespace + ":BoolTestCustomParameter-1.0.0";
            ParameterDefinition customParameter = ParameterDefinition.Create(schemaId, ParameterDataType.Bool);
            customParameter.Name = "Test";
            customParameter.SampleText = "";
            customParameter.Description = "";
            customParameter.ReadOnly = false;
            customParameter.IsCustomParameter = true;
            customParameter.GroupID = Group.General.DisplayName();
            (customParameter as BoolParameterDefinition).Value = true;
            await element.CreateInstanceParameterAsync(customParameter);
        }

        private async Task CreateCustomInstanceParameter_Float(Element element, string schemaNamespace)
        {
            string schemaId = "exchange.parameter." + schemaNamespace + ":Float64TestCustomParameter-1.0.0";
            ParameterDefinition customParameterFloat = ParameterDefinition.Create(schemaId, ParameterDataType.Float64);
            customParameterFloat.Name = "TestFloat64";
            customParameterFloat.SampleText = "SampleText-FloatCustomParam";
            customParameterFloat.Description = "Description-FloatCustomParam";
            customParameterFloat.ReadOnly = false;
            customParameterFloat.GroupID = Group.Dimensions.DisplayName();
            customParameterFloat.SpecID = Spec.Volume.DisplayName();
            customParameterFloat.IsCustomParameter = true;
            (customParameterFloat as MeasurableParameterDefinition).Value = 4.52;
            await element.CreateInstanceParameterAsync(customParameterFloat);
        }

        private async Task CreateCustomInstanceParameter_Int(Element element, string schemaNamespace)
        {
            string schemaId = "exchange.parameter." + schemaNamespace + ":Int64TestCustomParameter-1.0.0";
            ParameterDefinition customParameterInt = ParameterDefinition.Create(schemaId, ParameterDataType.Int64);
            customParameterInt.Name = "TestInt64";
            customParameterInt.SampleText = "";
            customParameterInt.Description = "";
            customParameterInt.ReadOnly = false;
            customParameterInt.GroupID = Group.General.DisplayName();
            customParameterInt.IsCustomParameter = true;
            (customParameterInt as Int64ParameterDefinition).Value = 5;
            await element.CreateInstanceParameterAsync(customParameterInt);
        }

        private async Task CreateCustomInstanceParameter_String(Element element, string schemaNamespace)
        {
            string schemaId = "exchange.parameter." + schemaNamespace + ":StringTestCustomParameter-1.0.0";
            ParameterDefinition customParameterString = ParameterDefinition.Create(schemaId, ParameterDataType.String);
            customParameterString.Name = "TestString";
            customParameterString.SampleText = "SampleTest-String";
            customParameterString.Description = "Description-String";
            customParameterString.ReadOnly = false;
            customParameterString.GroupID = Group.Graphics.DisplayName();
            customParameterString.IsCustomParameter = true;
            (customParameterString as StringParameterDefinition).Value = "TestStringValue";
            await element.CreateInstanceParameterAsync(customParameterString);
        }

        private async Task CreateCustomDesignParameter_Bool(ElementDataModel element, string schemaNamespace)
        {
            string schemaId = "exchange.parameter." + schemaNamespace + ":BoolTestCustomTypeParameter-1.0.0";
            ParameterDefinition customParameterTestDesign = ParameterDefinition.Create(schemaId, ParameterDataType.Bool);
            customParameterTestDesign.Name = "Test_Design_Param";
            customParameterTestDesign.SampleText = "";
            customParameterTestDesign.Description = "";
            customParameterTestDesign.ReadOnly = false;
            customParameterTestDesign.GroupID = Group.Graphics.DisplayName();
            customParameterTestDesign.IsCustomParameter = true;
            (customParameterTestDesign as BoolParameterDefinition).Value = true;
            await element.CreateTypeParameterAsync("Generic Object", customParameterTestDesign);
        }

        private async Task CreateCustomDesignParameter_Float(ElementDataModel element, string schemaNamespace)
        {
            string schemaId = "exchange.parameter." + schemaNamespace + ":Float64TestCustomTypeParameter-1.0.0";
            ParameterDefinition customParameterFloatDesign = ParameterDefinition.Create(schemaId, ParameterDataType.Float64);
            customParameterFloatDesign.Name = "TestFloat64_Desig_Param";
            customParameterFloatDesign.SampleText = "SampleText-FloatCustomParam";
            customParameterFloatDesign.Description = "Description-FloatCustomParam";
            customParameterFloatDesign.ReadOnly = false;
            customParameterFloatDesign.GroupID = Group.Dimensions.DisplayName();
            customParameterFloatDesign.SpecID = Spec.Volume.DisplayName();
            customParameterFloatDesign.IsCustomParameter = true;
            (customParameterFloatDesign as MeasurableParameterDefinition).Value = 4.52;
            await element.CreateTypeParameterAsync("Generic Object", customParameterFloatDesign);
        }

        private async Task CreateCustomDesignParameter_Int(ElementDataModel element, string schemaNamespace)
        {
            string schemaId = "exchange.parameter." + schemaNamespace + ":Int64TestCustomTypeParameter-1.0.0";
            ParameterDefinition customParameterIntDesign = ParameterDefinition.Create(schemaId, ParameterDataType.Int64);
            customParameterIntDesign.Name = "TestInt64_Design_Param";
            customParameterIntDesign.SampleText = "SampleText-Int64CustomParam";
            customParameterIntDesign.Description = "Desc-Int64CustomParam";
            customParameterIntDesign.ReadOnly = false;
            customParameterIntDesign.GroupID = Group.Graphics.DisplayName();
            customParameterIntDesign.IsCustomParameter = true;
            (customParameterIntDesign as Int64ParameterDefinition).Value = 5;
            await element.CreateTypeParameterAsync("Generic Object", customParameterIntDesign);
        }

        private async Task CreateCustomDesignParameter_String(ElementDataModel element, string schemaNamespace)
        {
            string schemaId = "exchange.parameter." + schemaNamespace + ":StringTestCustomTypeParameter-1.0.0";
            ParameterDefinition customParameterStringDesign = ParameterDefinition.Create(schemaId, ParameterDataType.String);
            customParameterStringDesign.Name = "TestString-Design";
            customParameterStringDesign.SampleText = "SampleTest-String-Design";
            customParameterStringDesign.Description = "Description-String-Design";
            customParameterStringDesign.ReadOnly = false;
            customParameterStringDesign.GroupID = Group.Graphics.DisplayName();
            customParameterStringDesign.IsCustomParameter = true;
            (customParameterStringDesign as StringParameterDefinition).Value = "TestStringValue";
            await element.CreateTypeParameterAsync("Generic Object", customParameterStringDesign);
        }

        public async Task AddInstanceParametersToElement(Element element)
        {
            //add element instance parameter
            var hostAreaComputed = ParameterDefinition.Create(Autodesk.Parameters.Parameter.HostAreaComputed, ParameterDataType.Float64);
            (hostAreaComputed as MeasurableParameterDefinition).Value = 4.684312000000002;
            await element.CreateInstanceParameterAsync(hostAreaComputed);

            ParameterDefinition relatedToMass = ParameterDefinition.Create(Autodesk.Parameters.Parameter.RelatedToMass, ParameterDataType.Bool);
            (relatedToMass as BoolParameterDefinition).Value = true;
            await element.CreateInstanceParameterAsync(relatedToMass);

            ParameterDefinition wallStructuralUsageParam = ParameterDefinition.Create(Autodesk.Parameters.Parameter.WallStructuralUsageParam, ParameterDataType.Int64);
            (wallStructuralUsageParam as Int64ParameterDefinition).Value = 42;
            await element.CreateInstanceParameterAsync(wallStructuralUsageParam);

            ParameterDefinition ifcGuid = ParameterDefinition.Create(Autodesk.Parameters.Parameter.IfcGuid, ParameterDataType.String);
            (ifcGuid as StringParameterDefinition).Value = "0q69lF83X65vuO5PXJfXpH";
            await element.CreateInstanceParameterAsync(ifcGuid);

            ParameterDefinition wallCrossSection = ParameterDefinition.Create(Autodesk.Parameters.Parameter.WallCrossSection, ParameterDataType.Int32);
            (wallCrossSection as Int32ParameterDefinition).Value = 1;
            await element.CreateInstanceParameterAsync(wallCrossSection);

            //add element instance reference parameter
            await element.CreateReferenceParameterAsync(Autodesk.Parameters.Parameter.WallBaseConstraint, "1C4F1B4A52597F316FE15C0533238314EEA43E75");
            //add element instance reference Name Only parameter
            await element.CreateReferenceNameOnlyParametersAsync(Autodesk.Parameters.Parameter.PhaseCreated, "New Construction");
        }

        public void AddElementsForExchangeUpdate(ElementDataModel data)
        {
            //Add Element with BRep Geometry
            var newBRepGeometry = new List<ElementGeometry>();
            var filePath = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\InputStepFile\\nist_ftc_09_asme1_rd.stp";
            newBRepGeometry.Add(ElementDataModel.CreateGeometry(new GeometryProperties(filePath, commonRenderStyle)));
            var newBRep = data.AddElement(new ElementProperties("0-new", "SampleBrep", "Generics", "Generic", "Non-Generic Object"));
            data.SetElementGeometryByElement(newBRep, newBRepGeometry);

            //Add Element with Mesh Geometry
            var newMeshElement = data.AddElement(new ElementProperties("MeshElementUpdate", "SampleMesh", "GenericsMeshUpdate", "GenericMeshUpdate", "Mesh Object Update"));
            var newMeshGeometry = new List<ElementGeometry>();
            var filePathToMesh = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\InputStepFile\\mesh2.obj";
            newMeshGeometry.Add(ElementDataModel.CreateGeometry(new GeometryProperties(filePathToMesh, commonRenderStyle)));
            data.SetElementGeometryByElement(newMeshElement, newMeshGeometry);
        }

        private void AddPrimitivePolylineGeometry(ElementDataModel dataModel)
        {
            var polyLineElement = dataModel.AddElement(new ElementProperties("Polyline", "SamplePolyline", "PolylineGenerics", "PolylineGeneric", "PolylineElement"));
            var polyLineElementGeometry = new List<ElementGeometry>();
            var geomContainer = new GeometryContainer()
            {
                Curves = new CurveArray()
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
                        }
                    }
                }
            };

            polyLineElementGeometry.Add(ElementDataModel.CreatePrimitiveGeometry(new GeometryProperties(geomContainer, commonRenderStyle)));
            dataModel.SetElementGeometryByElement(polyLineElement, polyLineElementGeometry);
        }
    }
}
