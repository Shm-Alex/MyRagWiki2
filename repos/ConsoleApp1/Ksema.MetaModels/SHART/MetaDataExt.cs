using System.Collections.Generic;
using Framework.Utils;

namespace Shart.ModelDef
{
    public static class MetaDataExt
    {
        public static ICollection<IIndexMetaData> GetIndexes(this IOLAPObjectMetaData obj)
        {
            return ((OLAPObjectMetaData)obj).GetIndexes();
        }

        public static DatabaseDataSpace MainTableFilegroup(this IOLAPObjectMetaData obj)
        {
            return ((OLAPObjectMetaData)obj).MainTableFilegroup;
        }

		public static DatabaseDataSpace ViewTableFilegroup( this IOLAPObjectMetaData obj )
        {
            return ((OLAPObjectMetaData)obj).ViewTableFilegroup;
        }

        public static ShartModelDTO ConvertToDTO(this IShartModelMetaData model)
        {
            ShartModelDTO modelDTO = new ShartModelDTO();
            modelDTO.AssemblyName = model.AssemblyName;
            modelDTO.DefaultBackRefName = model.DefaultBackRefName;
            modelDTO.DefaultClassName = model.DefaultClassName;
            modelDTO.DefaultRefName = model.DefaultRefName;
            modelDTO.DefaultSetName = model.DefaultSetName;
            modelDTO.Name = model.Name;
            modelDTO.Namespace = model.Namespace;

            model.GetAllCubesMetaData().ForEach(
                cube => modelDTO.Contents.Add(cube.Name, cube.ConvertToDTO()));
            model.GetAllDimensionsMetaData().ForEach(
                dim => modelDTO.Contents.Add(dim.Name, dim.ConvertToDTO()));

            return modelDTO;
        }

        static void ConvertToDTO(IOLAPObjectMetaData obj, OLAPObjectDTO dto)
        {
            dto.ClassName = obj.ClassName;
            dto.Description = obj.Description;
            dto.GUID = obj.GUID;
            dto.IDName = obj.IDName;
            dto.IsHistoric = obj.IsHistoric;
            dto.IsSystem = obj.IsSystem;
            dto.Name = obj.Name;
            dto.NameTemplate = obj.NameTemplate;
            dto.SetName = obj.SetName;
            dto.SetTemplate = obj.SetTemplate;
            dto.TablePostfix = obj.TablePostfix;
            dto.Version = obj.Version;
            dto.ModelName = obj.ModelName;

            obj.GetDataFields().ForEach(
                f => dto.Fields.Add(f.Name, f.ConvertToDTO()));
            obj.GetReferences().ForEach(
                r => dto.Fields.Add(r.Name, r.ConvertToDTO()));
        }

        static CubeDTO ConvertToDTO(this ICubeMetaData cube)
        {
            CubeDTO cubeDTO = new CubeDTO();
            ConvertToDTO(cube, cubeDTO);
            return cubeDTO;
        }

        static DimensionDTO ConvertToDTO(this IDimensionMetaData dim)
        {
            DimensionDTO dimDTO = new DimensionDTO();
            ConvertToDTO(dim, dimDTO);

            dimDTO.LevelCount = dim.LevelCount;
            dimDTO.IsAccessControlled = dim.IsAccessControlled;
            dimDTO.HasReferenceRoot = dim.HasReferenceRoot;
            dimDTO.NeedSearchIndexing = dim.NeedSearchIndexing;

            return dimDTO;
        }

        static void ConvertToDTO(this IFieldMetaData field, FieldDTO fieldDTO)
        {
            fieldDTO.DataSize = field.DataSize;
            fieldDTO.DataType = field.DataType;
            fieldDTO.Description = field.Description;
            fieldDTO.GUID = field.GUID;
            fieldDTO.Name = field.Name;
            fieldDTO.NameTemplate = field.NameTemplate;
            fieldDTO.RuntimeDataTypeName = field.RuntimeDataTypeName;
            fieldDTO.Version = field.Version;
			fieldDTO.IsComputed = field.IsComputed;
			fieldDTO.Formula = field.Formula;
        }

        static DataFieldDTO ConvertToDTO(this IDataFieldMetaData field)
        {
            DataFieldDTO fieldDTO = new DataFieldDTO();
            ConvertToDTO(field, fieldDTO);
            fieldDTO.IsAnalysis = field.IsAnalysis;
            fieldDTO.IsIndexed = field.IsIndexed;
            return fieldDTO;
        }

        static ReferenceDTO ConvertToDTO(this IReferenceMetaData aRef)
        {
            ReferenceDTO refDTO = new ReferenceDTO();
            ConvertToDTO(aRef, refDTO);
            refDTO.IsIdentifying = aRef.IsIdentifying;
            refDTO.BackRefTemplate = aRef.BackRefTemplate;
            refDTO.BackRefPropertyName = aRef.BackRefPropertyName;
            refDTO.HasBackRef = aRef.HasBackRef;
            refDTO.TargetName = aRef.TargetName;

            return refDTO;
        }
    }
}