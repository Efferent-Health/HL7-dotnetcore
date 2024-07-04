using System;
using System.Collections.Generic;

namespace HL7.Dotnetcore
{
    public class Segment : MessageElement
    {
        internal FieldCollection FieldList { get; set; }
        internal int SequenceNo { get; set; }
                
        public string Name { get; set; }

        public Segment(HL7Encoding encoding)
        {
            this.FieldList = new FieldCollection();
            this.Encoding = encoding;
        }

        public Segment(string name, HL7Encoding encoding)
        {
            this.FieldList = new FieldCollection();
            this.Name = name;
            this.Encoding = encoding;
        }

        protected override void ProcessValue()
        {
            var allFields = _value.Split(this.Encoding.FieldDelimiter);
            
            for (int i = 1; i < allFields.Length; i++)
            {
                string strField = allFields[i];
                Field field = new Field(this.Encoding);   

                if (Name == "MSH" && i == 1)
                    field.IsDelimitersField = true; // special case

                field.Value = strField;
                this.FieldList.Add(field);
            }

            if (this.Name == "MSH")
            {
                var field1 = new Field(this.Encoding);
                field1.IsDelimitersField = true;
                field1.Value = this.Encoding.FieldDelimiter.ToString();

                this.FieldList.Insert(0,field1);
            }
        }

        public Segment DeepCopy()
        {
            var newSegment = new Segment(this.Name, this.Encoding);
            newSegment.Value = this.Value; 

            return newSegment;        
        }

        public void AddEmptyField()
        {
            this.AddNewField(string.Empty);
        }

        public void AddNewField(string content, int position = -1)
        {
            this.AddNewField(new Field(content, this.Encoding), position);
        }

        public void AddNewField(string content, bool isDelimiters)
        {
            var newField = new Field(this.Encoding);

            if (isDelimiters)
                newField.IsDelimitersField = true; // Prevent decoding

            newField.Value = content;
            this.AddNewField(newField, -1);
        }

        public bool AddNewField(Field field, int position = -1)
        {
            try
            {
                if (position < 0)
                {
                    this.FieldList.Add(field);
                }
                else 
                {
                    position = position - 1;
                    this.FieldList.Add(field, position);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                throw new HL7Exception("Unable to add new field in segment " + this.Name + " Error - " + ex.Message, ex);
            }
        }

        public Field Fields(int position)
        {
            position = position - 1;

            try
            {
                return this.FieldList[position];
            }
            catch (Exception ex)
            {
                throw new HL7Exception("Field not available Error - " + ex.Message, ex);
            }
        }

        public List<Field> GetAllFields()
        {
            return this.FieldList;
        }

        public int GetSequenceNo()
        {
            return this.SequenceNo;
        }
    }
}
