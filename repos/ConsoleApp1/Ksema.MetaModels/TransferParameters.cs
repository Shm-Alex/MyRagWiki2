using System.Runtime.Serialization;

namespace Shart.Shared.Searching
{
    [DataContract]
    public class TransferParameters
    {
        [DataMember]
        public string _TableName ;
        public string TableName
        {
            get { return _TableName; }
            set { _TableName = value; }
        }

        [DataMember]
        public string _columns = "*";
        public string Columns
        {
            get { return _columns; }
            set { _columns = value; }
        }

        public override int GetHashCode()
        {
            return _columns.GetHashCode() ^ _TableName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            TransferParameters prs = obj as TransferParameters;
            if (prs != null)
                return prs._columns == this._columns && prs._TableName == this._TableName;
            else
                return obj.Equals(this);
        }
    }
}
