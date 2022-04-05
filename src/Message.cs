using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HL7.Dotnetcore
{
    public class Message
    {
        private List<string> allSegments = null;
        internal Dictionary<string, List<Segment>> SegmentList { get; set; } = new Dictionary<string, List<Segment>>();

        public string HL7Message { get; set; }
        public string Version { get; set; }
        public string MessageStructure { get; set; }
        public string MessageControlID { get; set; }
        public string ProcessingID { get; set; }
        public int SegmentCount { get; set; }
        public HL7Encoding Encoding { get; set; } = new HL7Encoding();

        private const string segmentRegex = @"^([A-Z][A-Z][A-Z1-9])([\(\[]([0-9]+)[\)\]]){0,1}$";
        private const string fieldRegex = @"^([0-9]+)([\(\[]([0-9]+)[\)\]]){0,1}$";
        private const string otherRegEx = @"^[1-9]([0-9]{1,2})?$";

        public Message()
        {
        }

        public Message(string strMessage)
        {
            HL7Message = strMessage;
        }

        public override bool Equals(object obj)
        {
            if (obj is Message)
                return this.Equals((obj as Message).HL7Message);

            if (obj is string)
            {
                var arr1 = MessageHelper.SplitString(this.HL7Message, this.Encoding.SegmentDelimiter, StringSplitOptions.RemoveEmptyEntries);
                var arr2 = MessageHelper.SplitString(obj as string, this.Encoding.SegmentDelimiter, StringSplitOptions.RemoveEmptyEntries);

                return arr1.SequenceEqual(arr2);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.HL7Message.GetHashCode();
        }

        /// <summary>
        /// Parse the HL7 message in text format, throws HL7Exception if error occurs
        /// </summary>
        /// <param name="bypassValidation">To parse the message without any validation</param>
        /// <returns>boolean</returns>
        public bool ParseMessage(bool bypassValidation = false)
        {
            bool isValid = false;
            bool isParsed = false;

            try
            {
                if (!bypassValidation)
                {
                    isValid = this.validateMessage();
                }
                else { isValid = true; }
            }
            catch (HL7Exception ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new HL7Exception("Unhandled Exception in validation - " + ex.Message, HL7Exception.BAD_MESSAGE);
            }

            if (isValid)
            {
                try
                {
                    if (this.allSegments == null || this.allSegments.Count <= 0)
                        this.allSegments = MessageHelper.SplitMessage(HL7Message);

                    int segSeqNo = 0;

                    foreach (string strSegment in this.allSegments)
                    {
                        if (string.IsNullOrWhiteSpace(strSegment))
                            continue;

                        Segment newSegment = new Segment(this.Encoding)
                        {
                            Name = strSegment.Substring(0, 3),
                            Value = strSegment,
                            SequenceNo = segSeqNo++
                        };

                        this.AddNewSegment(newSegment);
                    }

                    this.SegmentCount = segSeqNo;

                    string strSerializedMessage = string.Empty;

                    try
                    {
                        strSerializedMessage = this.SerializeMessage(false); 
                    }
                    catch (HL7Exception ex)
                    {
                        throw new HL7Exception("Failed to serialize parsed message with error - " + ex.Message, HL7Exception.PARSING_ERROR);
                    }

                    if (!string.IsNullOrEmpty(strSerializedMessage))
                    {
                        if (this.Equals(strSerializedMessage))
                            isParsed = true;
                    }
                    else
                    {
                        throw new HL7Exception("Unable to serialize to original message - ", HL7Exception.PARSING_ERROR);
                    }
                }
                catch (Exception ex)
                {
                    throw new HL7Exception("Failed to parse the message with error - " + ex.Message, HL7Exception.PARSING_ERROR);
                }
            }
            
            return isParsed;
        }

        /// <summary>
        /// Serialize the message in text format
        /// </summary>
        /// <param name="validate">Validate the message before serializing</param>
        /// <returns>string with HL7 message</returns>
        public string SerializeMessage(bool validate)
        {
            if (validate && !this.validateMessage())
                throw new HL7Exception("Failed to validate the updated message", HL7Exception.BAD_MESSAGE);

            var strMessage = new StringBuilder();
            string currentSegName = string.Empty;
            List<Segment> _segListOrdered = getAllSegmentsInOrder();

            try
            {
                try
                {
                    foreach (Segment seg in _segListOrdered)
                    {
                        currentSegName = seg.Name;

                        strMessage.Append(seg.Name);
                        
                        if (seg.FieldList.Count > 0)
                            strMessage.Append(Encoding.FieldDelimiter);

                        int startField = currentSegName == "MSH" ? 1 : 0;

                        for (int i = startField; i<seg.FieldList.Count; i++)
                        {
                            if (i > startField)
                                strMessage.Append(Encoding.FieldDelimiter);

                            var field = seg.FieldList[i];

                            if (field.IsDelimiters)
                            {
                                strMessage.Append(field.Value);
                                continue;
                            }

                            if (field.HasRepetitions)
                            {
                                for (int j = 0; j < field.RepetitionList.Count; j++)
                                {
                                    if (j > 0)
                                        strMessage.Append(Encoding.RepeatDelimiter);

                                    serializeField(field.RepetitionList[j], strMessage);
                                }
                            }
                            else
                                serializeField(field, strMessage);
                        }
                        
                        strMessage.Append(Encoding.SegmentDelimiter);
                    }
                }
                catch (Exception ex)
                {
                    if (currentSegName == "MSH")
                        throw new HL7Exception("Failed to serialize the MSH segment with error - " + ex.Message, HL7Exception.SERIALIZATION_ERROR);
                    else 
                        throw;
                }

                return strMessage.ToString();
            }
            catch (Exception ex)
            {
                throw new HL7Exception("Failed to serialize the message with error - " + ex.Message, HL7Exception.SERIALIZATION_ERROR);
            }
        }

        /// <summary>
        /// Get the Value of specific Field/Component/SubCpomponent, throws error if field/component index is not valid
        /// </summary>
        /// <param name="strValueFormat">Field/Component position in format SEGMENTNAME.FieldIndex.ComponentIndex.SubComponentIndex example PID.5.2</param>
        /// <returns>Value of specified field/component/subcomponent</returns>
        public string GetValue(string strValueFormat)
        {
            string segmentName = string.Empty;
            int segmentNumber = 0;
            int componentIndex = 0;
            int subComponentIndex = 0;
            string strValue = string.Empty;
            List<string> allComponents = MessageHelper.SplitString(strValueFormat, new char[] { '.' });

            int comCount = allComponents.Count;
            bool isValid = validateValueFormat(allComponents);

            if (isValid)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(allComponents[0], segmentRegex);
                if (matches.Count < 1)
                    throw new HL7Exception("Request format is not valid: " + strValueFormat);

                segmentName = matches[0].Groups[1].Value;
                if (matches[0].Length > 3) 
                {
                    Int32.TryParse(matches[0].Groups[3].Value, out segmentNumber);
                    segmentNumber--;
                }

                if (SegmentList.ContainsKey(segmentName))
                {

                    var segment = SegmentList[segmentName][segmentNumber];

                    if (comCount == 4)
                    {
                        Int32.TryParse(allComponents[2], out componentIndex);
                        Int32.TryParse(allComponents[3], out subComponentIndex);

                        try
                        {
                            var field = this.getField(segment, allComponents[1]);
                            strValue = field.ComponentList[componentIndex - 1].SubComponentList[subComponentIndex - 1].Value;
                        }
                        catch (Exception ex)
                        {
                            throw new HL7Exception("SubComponent not available - " + strValueFormat + " Error: " + ex.Message);
                        }
                    }
                    else if (comCount == 3)
                    {
                        Int32.TryParse(allComponents[2], out componentIndex);

                        try
                        {
                            var field = this.getField(segment, allComponents[1]);
                            strValue = field.ComponentList[componentIndex - 1].Value;
                        }
                        catch (Exception ex)
                        {
                            throw new HL7Exception("Component not available - " + strValueFormat + " Error: " + ex.Message);
                        }
                    }
                    else if (comCount == 2)
                    {
                        try
                        {
                            var field = this.getField(segment, allComponents[1]);
                            strValue = field.Value;
                        }
                        catch (Exception ex)
                        {
                            throw new HL7Exception("Field not available - " + strValueFormat + " Error: " + ex.Message);
                        }
                    }
                    else
                    {
                        try
                        {
                            strValue = segment.Value;
                        }
                        catch (Exception ex)
                        {
                            throw new HL7Exception("Segment value not available - " + strValueFormat + " Error: " + ex.Message);
                        }
                    }
                }
                else
                {
                    throw new HL7Exception("Segment name not available: " + strValueFormat);
                }
            }
            else
            {
                throw new HL7Exception("Request format is not valid: " + strValueFormat);
            }

            return this.Encoding.Decode(strValue);
        }

        /// <summary>
        /// Sets the Value of specific Field/Component/SubComponent in matching Segments, throws error if field/component index is not valid
        /// </summary>
        /// <param name="strValueFormat">Field/Component position in format SEGMENTNAME.FieldIndex.ComponentIndex.SubComponentIndex example PID.5.2</param>
        /// <param name="strValue">Value for the specified field/component</param>
        /// <returns>boolean</returns>
        public bool SetValue(string strValueFormat, string strValue)
        {
            bool isSet = false;
            string segmentName = string.Empty;
            int componentIndex = 0;
            int subComponentIndex = 0;
            List<string> allComponents = MessageHelper.SplitString(strValueFormat, new char[] { '.' });
            int comCount = allComponents.Count;
            bool isValid = validateValueFormat(allComponents);

            if (isValid)
            {
                segmentName = allComponents[0];

                if (SegmentList.ContainsKey(segmentName))
                {
                    foreach (var segment in SegmentList[segmentName])
                    {
                        if (comCount == 4)
                        {
                            Int32.TryParse(allComponents[2], out componentIndex);
                            Int32.TryParse(allComponents[3], out subComponentIndex);

                            try
                            {
                                var field = this.getField(segment, allComponents[1]);
                                field.ComponentList[componentIndex - 1].SubComponentList[subComponentIndex - 1].Value = strValue;
                                isSet = true;
                            }
                            catch (Exception ex)
                            {
                                throw new HL7Exception("SubComponent not available - " + strValueFormat + " Error: " + ex.Message);
                            }
                        }
                        else if (comCount == 3)
                        {
                            Int32.TryParse(allComponents[2], out componentIndex);

                            try
                            {
                                var field = this.getField(segment, allComponents[1]);
                                field.ComponentList[componentIndex - 1].Value = strValue;
                                isSet = true;
                            }
                            catch (Exception ex)
                            {
                                throw new HL7Exception("Component not available - " + strValueFormat + " Error: " + ex.Message);
                            }
                        }
                        else if (comCount == 2)
                        {
                            try
                            {
                                var field = this.getField(segment, allComponents[1]);
                                field.Value = strValue;
                                isSet = true;
                            }
                            catch (Exception ex)
                            {
                                throw new HL7Exception("Field not available - " + strValueFormat + " Error: " + ex.Message);
                            }
                        }
                        else
                        {
                            throw new HL7Exception("Cannot overwrite a segment value");
                        }
                    }

                }
                else
                    throw new HL7Exception("Segment name not available");
            }
            else
                throw new HL7Exception("Request format is not valid");

            return isSet;
        }

        /// <summary>
        /// check if specified field has components
        /// </summary>
        /// <param name="strValueFormat">Field/Component position in format SEGMENTNAME.FieldIndex.ComponentIndex.SubComponentIndex example PID.5.2</param>
        /// <returns>boolean</returns>
        public bool IsComponentized(string strValueFormat)
        {
            bool isComponentized = false;
            string segmentName = string.Empty;
            List<string> allComponents = MessageHelper.SplitString(strValueFormat, new char[] { '.' });
            int comCount = allComponents.Count;
            bool isValid = validateValueFormat(allComponents);

            if (isValid)
            {
                segmentName = allComponents[0];

                if (comCount >= 2)
                {
                    try
                    {
                        var segment = SegmentList[segmentName].First();
                        var field = this.getField(segment, allComponents[1]);

                        isComponentized = field.IsComponentized;
                    }
                    catch (Exception ex)
                    {
                        throw new HL7Exception("Field not available - " + strValueFormat + " Error: " + ex.Message);
                    }
                }
                else
                    throw new HL7Exception("Field not identified in request");
            }
            else
                throw new HL7Exception("Request format is not valid");

            return isComponentized;
        }

        /// <summary>
        /// check if specified fields has repeatitions
        /// </summary>
        /// <param name="strValueFormat">Field/Component position in format SEGMENTNAME.FieldIndex.ComponentIndex.SubComponentIndex example PID.5.2</param>
        /// <returns>boolean</returns>
        public bool HasRepetitions(string strValueFormat)
        {
            bool hasRepetitions = false;
            string segmentName = string.Empty;
            List<string> allComponents = MessageHelper.SplitString(strValueFormat, new char[] { '.' });
            int comCount = allComponents.Count;
            bool isValid = validateValueFormat(allComponents);

            if (isValid)
            {
                segmentName = allComponents[0];

                if (comCount >= 2)
                {
                    try
                    {
                        var segment = SegmentList[segmentName].First();
                        var field = this.getField(segment, allComponents[1]);

                        hasRepetitions = field.HasRepetitions;
                    }
                    catch (Exception ex)
                    {
                        throw new HL7Exception("Field not available - " + strValueFormat + " Error: " + ex.Message);
                    }
                }
                else
                    throw new HL7Exception("Field not identified in request");
            }
            else
                throw new HL7Exception("Request format is not valid");

            return hasRepetitions;
        }

        /// <summary>
        /// check if specified component has sub components
        /// </summary>
        /// <param name="strValueFormat">Field/Component position in format SEGMENTNAME.FieldIndex.ComponentIndex.SubComponentIndex example PID.5.2</param>
        /// <returns>boolean</returns>
        public bool IsSubComponentized(string strValueFormat)
        {
            bool isSubComponentized = false;
            string segmentName = string.Empty;
            int componentIndex = 0;
            List<string> allComponents = MessageHelper.SplitString(strValueFormat, new char[] { '.' });
            int comCount = allComponents.Count;
            bool isValid = validateValueFormat(allComponents);

            if (isValid)
            {
                segmentName = allComponents[0];

                if (comCount >= 3)
                {
                    try
                    {
                        var segment = SegmentList[segmentName].First();
                        var field = this.getField(segment, allComponents[1]);

                        Int32.TryParse(allComponents[2], out componentIndex);
                        isSubComponentized = field.ComponentList[componentIndex - 1].IsSubComponentized;
                    }
                    catch (Exception ex)
                    {
                        throw new HL7Exception("Component not available - " + strValueFormat + " Error: " + ex.Message);
                    }
                }
                else
                    throw new HL7Exception("Component not identified in request");
            }
            else
                throw new HL7Exception("Request format is not valid");

            return isSubComponentized;
        }

        /// <summary>
        /// Builds the acknowledgement message for this message
        /// </summary>
        /// <param name="bypassValidation">Bypasses validation of the resulting ACK message</param>
        /// <returns>An ACK message if success, otherwise null</returns>
        public Message GetACK(bool bypassValidation = false)
        {
            return this.createAckMessage("AA", false, null, bypassValidation);
        }

        /// <summary>
        /// Builds a negative ack for this message
        /// </summary>
        /// <param name="code">ack code like AR, AE</param>
        /// <param name="errMsg">Error message to be sent with NACK</param>
        /// <param name="bypassValidation">Bypasses validation of the resulting NACK message</param>
        /// <returns>A NACK message if success, otherwise null</returns>
        public Message GetNACK(string code, string errMsg, bool bypassValidation = false)
        {
            return this.createAckMessage(code, true, errMsg, bypassValidation);
        }

        /// <summary>
        /// Adds a segment to the message
        /// </summary>
        /// <param name="newSegment">Segment to be appended to the end of the message</param>
        /// <returns>True if added successfully, otherwise false</returns>
        public bool AddNewSegment(Segment newSegment)
        {
            try
            {
                newSegment.SequenceNo = SegmentCount++;

                if (!SegmentList.ContainsKey(newSegment.Name))
                    SegmentList[newSegment.Name] = new List<Segment>();

                SegmentList[newSegment.Name].Add(newSegment);
                return true;
            }
            catch (Exception ex)
            {
                SegmentCount--;
                throw new HL7Exception("Unable to add new segment. Error - " + ex.Message);
            }
        }

        /// <summary>
        /// Removes a segment from the message
        /// </summary>
        /// <param name="segmentName">Segment to be removed</param>
        /// <param name="index">Zero-based index of the segment to be removed, in case of multiple. Default is 0.</param>
        /// <returns>True if found and removed successfully, otherwise false</returns>
        public bool RemoveSegment(string segmentName, int index = 0) 
        {
            try
            {
                if (!SegmentList.ContainsKey(segmentName))
                    return false;

                var list = SegmentList[segmentName];
                if (list.Count <= index)
                    return false;

                list.RemoveAt(index);
                SegmentCount--;

                return true;
            }
            catch (Exception ex)
            {
                throw new HL7Exception("Unable to add remove segment. Error - " + ex.Message);
            }
        }

        public List<Segment> Segments()
        {
            return getAllSegmentsInOrder();
        }

        public List<Segment> Segments(string segmentName)
        {
            return getAllSegmentsInOrder().FindAll(o=> o.Name.Equals(segmentName));
        }

        public Segment DefaultSegment(string segmentName)
        {
            return getAllSegmentsInOrder().First(o => o.Name.Equals(segmentName));
        }

        /// <summary>
        /// Addsthe header segment to a new message
        /// </summary>
        /// <param name="sendingApplication">Sending application name</param>
        /// <param name="sendingFacility">Sending facility name</param>
        /// <param name="receivingApplication">Receiving application name</param>
        /// <param name="receivingFacility">Receiving facility name</param>
        /// <param name="security">Security features. Can be null.</param>
        /// <param name="messageType">Message type ^ trigger event</param>
        /// <param name="messageControlID">Message control unique ID</param>
        /// <param name="processingID">Processing ID ^ processing mode</param>
        /// <param name="version">HL7 message version (2.x)</param>
        public void AddSegmentMSH(string sendingApplication, string sendingFacility, string receivingApplication, string receivingFacility,
            string security, string messageType, string messageControlID, string processingID, string version)
        {
                var dateString = MessageHelper.LongDateWithFractionOfSecond(DateTime.Now);
                var delim = this.Encoding.FieldDelimiter;

                string response = "MSH" + this.Encoding.AllDelimiters + delim + 
                    sendingApplication + delim + 
                    sendingFacility + delim +
                    receivingApplication + delim + 
                    receivingFacility + delim +
                    this.Encoding.Encode(dateString) + delim + 
                    (security ?? string.Empty) + delim + 
                    messageType + delim + 
                    messageControlID + delim +
                    processingID + delim + 
                    version + this.Encoding.SegmentDelimiter;

                var message = new Message(response);
                message.ParseMessage();
                this.AddNewSegment(message.DefaultSegment("MSH"));
        }

        /// <summary>
        /// Serialize to MLLP escaped byte array
        /// </summary>
        /// <param name="validate">Optional. Validate the message before serializing</param>
        /// <returns>MLLP escaped byte array</returns>
        public byte[] GetMLLP(bool validate = false)
        {
            string hl7 = this.SerializeMessage(validate);

            return MessageHelper.GetMLLP(hl7);
        }
        
        /// <summary>
        /// Builds an ACK or NACK message for this message
        /// </summary>
        /// <param name="code">ack code like AA, AR, AE</param>
        /// <param name="isNack">true for generating a NACK message, otherwise false</param>
        /// <param name="errMsg">error message to be sent with NACK</param>
        /// <param name="bypassValidation">Bypasses validation of the resulting ACK/NACK message</param>
        /// <returns>An ACK or NACK message if success, otherwise null</returns>
        private Message createAckMessage(string code, bool isNack, string errMsg, bool bypassValidation)
        {
            var response = new StringBuilder();

            if (this.MessageStructure != "ACK")
            {
                var dateString = MessageHelper.LongDateWithFractionOfSecond(DateTime.Now);
                var msh = this.SegmentList["MSH"].First();
                var delim = this.Encoding.FieldDelimiter;
                
                response.Append("MSH").Append(this.Encoding.AllDelimiters).Append(delim).Append(msh.FieldList[4].Value).Append(delim).Append(msh.FieldList[5].Value).Append(delim)
                    .Append(msh.FieldList[2].Value).Append(delim).Append(msh.FieldList[3].Value).Append(delim)
                    .Append(dateString).Append(delim).Append(delim).Append("ACK").Append(delim).Append(this.MessageControlID).Append(delim)
                    .Append(this.ProcessingID).Append(delim).Append(this.Version).Append(this.Encoding.SegmentDelimiter);
                
                response.Append("MSA").Append(delim).Append(code).Append(delim).Append(this.MessageControlID).Append((isNack ? delim + errMsg : string.Empty)).Append(this.Encoding.SegmentDelimiter);
            }
            else
            {
                return null;
            }

            try 
            {
                var message = new Message(response.ToString());
                message.ParseMessage(bypassValidation);
                return message;
            }
            catch 
            {
                return null;
            }
        }


        private Field getField(Segment segment, string index)
        {
            int repetition = 0;
            var matches = System.Text.RegularExpressions.Regex.Matches(index, fieldRegex);

            if (matches.Count < 1)
                throw new Exception("Invalid field index");

            Int32.TryParse(matches[0].Groups[1].Value, out int fieldIndex);
            fieldIndex--;

            if (matches[0].Length > 3)
            {
                Int32.TryParse(matches[0].Groups[3].Value, out repetition);
                repetition--;
            }

            var field = segment.FieldList[fieldIndex];

            if (field.HasRepetitions)
                return field.RepetitionList[repetition];
            else if (repetition == 0)
                return field;
            else
                return null;
        }

        /// <summary>
        /// Validates the HL7 message for basic syntax
        /// </summary>
        /// <returns>A boolean indicating whether the whole message is valid or not</returns>
        private bool validateMessage()
        {
            try
            {
                if (!string.IsNullOrEmpty(HL7Message))
                {
                    // Check message length - MSH+Delimeters+12Fields in MSH
                    if (HL7Message.Length < 20)
                    {
                        throw new HL7Exception("Message Length too short: " + HL7Message.Length + " chars.", HL7Exception.BAD_MESSAGE);
                    }

                    // Check if message starts with header segment
                    if (!HL7Message.StartsWith("MSH"))
                    {
                        throw new HL7Exception("MSH segment not found at the beginning of the message", HL7Exception.BAD_MESSAGE);
                    }

                    this.Encoding.EvaluateSegmentDelimiter(this.HL7Message);
                    this.HL7Message = string.Join(this.Encoding.SegmentDelimiter, MessageHelper.SplitMessage(this.HL7Message)) + this.Encoding.SegmentDelimiter;

                    // Check Segment Name & 4th character of each segment
                    char fourthCharMSH = HL7Message[3];
                    this.allSegments = MessageHelper.SplitMessage(HL7Message);

                    foreach (string strSegment in this.allSegments)
                    {
                        if (string.IsNullOrWhiteSpace(strSegment))
                            continue;

                        string segmentName = strSegment.Substring(0, 3);
                        bool isValidSegmentName = System.Text.RegularExpressions.Regex.IsMatch(segmentName, segmentRegex);

                        if (!isValidSegmentName)
                        {
                            throw new HL7Exception("Invalid segment name found: " + strSegment, HL7Exception.BAD_MESSAGE);
                        }

                        if (strSegment.Length > 3 && fourthCharMSH != strSegment[3])
                        {
                            throw new HL7Exception("Invalid segment found: " + strSegment, HL7Exception.BAD_MESSAGE);
                        }
                    }

                    string _fieldDelimiters_Message = this.allSegments[0].Substring(3, 8 - 3);
                    this.Encoding.EvaluateDelimiters(_fieldDelimiters_Message);

                    // Count field separators, MSH.12 is required so there should be at least 11 field separators in MSH
                    int countFieldSepInMSH = this.allSegments[0].Count(f => f == Encoding.FieldDelimiter);

                    if (countFieldSepInMSH < 11)
                    {
                        throw new HL7Exception("MSH segment doesn't contain all the required fields", HL7Exception.BAD_MESSAGE);
                    }

                    // Find Message Version
                    var MSHFields = MessageHelper.SplitString(this.allSegments[0], Encoding.FieldDelimiter);

                    if (MSHFields.Count >= 12)
                    {
                        this.Version = MessageHelper.SplitString(this.Encoding.Decode(MSHFields[11]), Encoding.ComponentDelimiter)[0];
                    }
                    else
                    {
                        throw new HL7Exception("HL7 version not found in the MSH segment", HL7Exception.REQUIRED_FIELD_MISSING);
                    }

                    // Find Message Type & Trigger Event
                    try
                    {
                        string MSH_9 = this.Encoding.Decode(MSHFields[8]);

                        if (!string.IsNullOrEmpty(MSH_9))
                        {
                            var MSH_9_comps = MessageHelper.SplitString(MSH_9, this.Encoding.ComponentDelimiter);

                            if (MSH_9_comps.Count >= 3)
                            {
                                this.MessageStructure = MSH_9_comps[2];
                            }
                            else if (MSH_9_comps.Count > 0 && MSH_9_comps[0] != null && MSH_9_comps[0].Equals("ACK"))
                            {
                                this.MessageStructure = "ACK";
                            }
                            else if (MSH_9_comps.Count == 2)
                            {
                                this.MessageStructure = MSH_9_comps[0] + "_" + MSH_9_comps[1];
                            }
                            else
                            {
                                throw new HL7Exception("Message Type & Trigger Event value not found in message", HL7Exception.UNSUPPORTED_MESSAGE_TYPE);
                            }
                        }
                        else
                            throw new HL7Exception("MSH.9 not available", HL7Exception.UNSUPPORTED_MESSAGE_TYPE);
                    }
                    catch (System.IndexOutOfRangeException e)
                    {
                        throw new HL7Exception("Can't find message structure (MSH.9.3) - " + e.Message, HL7Exception.UNSUPPORTED_MESSAGE_TYPE);
                    }

                    try
                    {
                        this.MessageControlID = this.Encoding.Decode(MSHFields[9]);

                        if (string.IsNullOrEmpty(this.MessageControlID))
                            throw new HL7Exception("MSH.10 - Message Control ID not found", HL7Exception.REQUIRED_FIELD_MISSING);
                    }
                    catch (Exception ex)
                    {
                        throw new HL7Exception("Error occured while accessing MSH.10 - " + ex.Message, HL7Exception.REQUIRED_FIELD_MISSING);
                    }

                    try
                    {
                        this.ProcessingID = this.Encoding.Decode(MSHFields[10]);

                        if (string.IsNullOrEmpty(this.ProcessingID))
                            throw new HL7Exception("MSH.11 - Processing ID not found", HL7Exception.REQUIRED_FIELD_MISSING);
                    }
                    catch (Exception ex)
                    {
                        throw new HL7Exception("Error occured while accessing MSH.11 - " + ex.Message, HL7Exception.REQUIRED_FIELD_MISSING);
                    }
                }
                else
                    throw new HL7Exception("No Message Found", HL7Exception.BAD_MESSAGE);
            }
            catch (Exception ex)
            {
                throw new HL7Exception("Failed to validate the message with error - " + ex.Message, HL7Exception.BAD_MESSAGE);
            }

            return true;
        }

        /// <summary>
        /// Serializes a field into a string with proper encoding
        /// </summary>
        /// <returns>A serialized string</returns>
        private void serializeField(Field field, StringBuilder strMessage)
        {
            if (field.ComponentList.Count > 0)
            {
                int indexCom = 0;

                foreach (Component com in field.ComponentList)
                {
                    indexCom++;
                    if (com.SubComponentList.Count > 0)
                        strMessage.Append(string.Join(Encoding.SubComponentDelimiter.ToString(), com.SubComponentList.Select(sc => Encoding.Encode(sc.Value))));
                    else
                        strMessage.Append(Encoding.Encode(com.Value));

                    if (indexCom < field.ComponentList.Count)
                        strMessage.Append(Encoding.ComponentDelimiter);
                }
            }
            else
                strMessage.Append(Encoding.Encode(field.Value));

        }

        /// <summary> 
        /// Get all segments in order as they appear in original message. This the usual order: IN1|1 IN2|1 IN1|2 IN2|2
        /// </summary>
        /// <returns>A list of segments in the proper order</returns>
        private List<Segment> getAllSegmentsInOrder()
        {
            List<Segment> _list = new List<Segment>();

            foreach (string segName in SegmentList.Keys)
            {
                foreach (Segment seg in SegmentList[segName])
                {
                    _list.Add(seg);
                }
            }

            return _list.OrderBy(o => o.SequenceNo).ToList();
        }

        /// <summary>
        /// Validates the components of a value's position descriptor
        /// </summary>
        /// <returns>A boolean indicating whether all the components are valid or not</returns>
        private bool validateValueFormat(List<string> allComponents)
        {
            bool isValid = false;

            if (allComponents.Count > 0)
            {
                if (Regex.IsMatch(allComponents[0], segmentRegex))
                {
                    for (int i = 1; i < allComponents.Count; i++)
                    {
                        if (i == 1 && Regex.IsMatch(allComponents[i], fieldRegex))
                            isValid = true;
                        else if (i > 1 && Regex.IsMatch(allComponents[i], otherRegEx))
                            isValid = true;
                        else
                            return false;
                    }
                }
                else
                {
                    isValid = false;
                }
            }

            return isValid;
        }
    }
}
