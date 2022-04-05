# HL7-dotnetcore

[![NuGet](https://img.shields.io/nuget/v/HL7-dotnetcore.svg)](https://www.nuget.org/packages/HL7-dotnetcore/)
[![github](https://img.shields.io/github/stars/Efferent-Health/HL7-dotnetcore.svg)]()
[![Build Status](https://efferent.visualstudio.com/open-source/_apis/build/status/Efferent-Health.HL7-dotnetcore?branchName=master)](https://efferent.visualstudio.com/open-source/_build/latest?definitionId=8&branchName=master)

This is a lightweight library for building and parsing HL7 2.x messages, for .Net Standard and .Net Core. It is not tied to any particular version of HL7 nor validates against one. 

## Object construction

### Create a Message object and pass a raw HL7 message in text format

````cSharp
Message message = new Message(strMsg);

// Parse this message

bool isParsed = false;

try
{
    isParsed = message.ParseMessage();
}
catch(Exception ex)
{
    // Handle the exception
}
`````

### Adding a message header

For adding a header segment to a new message object, use the `AddSegmentMSH()` method, after constructing an empty message:

````cSharp
message.AddSegmentMSH(sendingApplication, sendingFacility, 
    receivingApplication, receivingFacility,
    security, messageType, 
    messageControlId, processingId, version);
````

&#9888;&#65039; Notice that every HL7 message needs a header segment to be considered valid

### Message extraction

If the HL7 message is coming from a MLLP connection (see [the official documentation](https://www.hl7.org/documentcenter/public/wg/inm/mllp_transport_specification.PDF)), the message needs to be cleared from the MLLP prefixes and suffixes. Also, consider there can be more than one message in a single MLLP frame.

For this purpose, there is an `ExtractMessages()` method, to be used as follows:

````cSharp
// extract the messages from a buffer containing a MLLP frame
var messages = MessageHelper.ExtractMessages(buffer);

// construct and process each message
foreach (var strMsg in messages)
{
    Message message = new Message(strMsg);
    message.ParseMessage(); // Required by most operations
    // do something with the message object
}
````
## Bypass Validation
````cSharp
Message  message = new Message(strMsg)
message.ParseMessage(true);
````
## Accessing Segments

### Get list of all segments

````cSharp
List<Segment> segList = message.Segments();
`````

### Get List of list of repeated segments by name 

For example if there are multiple IN1 segments

````cSharp
List<Segment> IN1List = message.Segments("IN1");
````

### Access a particular occurrence from multiple IN1s providing the index

Note index 1 will return the 2nd element from list

````cSharp
Segment IN1_2 = message.Segments("IN1")[1];
````

### Get count of IN1s

````cSharp
int countIN1 = message.Segments("IN1").Count;
````

### Access first occurrence of any segment

````cSharp
Segment IN1 = message.DefaultSegment("IN1");

// OR

Segment IN1 = message.Segments("IN1")[0];
````

## Accessing Fields

### Access field values

````cSharp
string SendingFacility = message.GetValue("MSH.4");

// OR

string SendingFacility = message.GetValue("MSH(1).4");

// OR

string SendingFacility = message.DefaultSegment("MSH").Fields(4).Value;

// OR

string SendingFacility = message.Segments("MSH")[0].Fields(4).Value;
`````

### Check if field is componentized

````cSharp
bool isComponentized = message.Segments("PID")[0].Fields(5).IsComponentized;

// OR

bool isComponentized = message.IsComponentized("PID.5");
`````

### Check if field has repetitions

````cSharp
bool isRepeated = message.Segments("PID")[0].Fields(3).HasRepetitions;

// OR

bool isRepeated = message.HasRepetitions("PID.3");
````

### Adding repeating field

````cSharp
var enc = new HL7Encoding();
Segment PID = new Segment("PID", enc);
Field f = new Field(enc);
f.HasRepetitions = true;

// Adding field f1 to f
Field f1 = new Field("A",enc);
f.AddRepeatingField(f1);

// Adding field f2 to f
Field f2 = new Field("B",enc);
f.AddRepeatingField(f2);
````

### Get list of repeated fields

````cSharp
List<Field> repList = message.Segments("PID")[0].Fields(3).Repetitions();
````

### Get particular repetition i.e 2nd repetition of PID.3

````cSharp
Field PID3_R2 = message.Segments("PID.3[2]");

// OR

Field PID3_R2 = message.Segments("PID.3(2)");
````

### Update value of any field i.e. to update PV1.2 – patient class

````cSharp
message.SetValue("PV1.2", "I");

// OR

message.Segments("PV1")[0].Fields(2).Value = "I";
````

### Access some of the required MSH fields with properties

````cSharp
string version = message.Version;
string msgControlID = message.MessageControlID;
string messageStructure = message.MessageStructure;
````

### Generate ACKs

To generate an ACK message

````cSharp
Message ack = message.GetACK();
````

To generate negative ACK (NACK) message with error message

````cSharp
Message nack = message.GetNACK("AR", "Invalid Processing ID");
````

It may be required to change the application and facility fields

````cSharp
Message ack = message.GetACK();
ack.SetValue("MSH.3", appName);
ack.SetValue("MSH.4", facility);

````

&#9888;&#65039; Take into account that a message shall be previously parsed before attempting to generate an ACK or NACK message.

## Accessing Components

### Access particular component i.e. PID.5.1 – Patient Family Name

````cSharp
string PatName1 = message.GetValue("PID.5.1");

// OR

string PatName1 = message.Segments("PID")[0].Fields(5).Components(1).Value;
````

### Check if component is sub componentized

````cSharp
bool isSubComponentized = message.Segments("PV1")[0].Fields(7).Components(1).IsSubComponentized;

// OR

bool isSubComponentized = message.IsSubComponentized("PV1.7.1");
````

### Update value of any component

````cSharp
message.Segments("PID")[0].Fields(5).Components(1).Value = "Jayant";

// OR

message.SetValue("PID.5.1", "Jayant");
````

### Adding new Segment

````cSharp
// Create a Segment with name ZIB
Segment newSeg = new Segment("ZIB");
 
// Create Field ZIB_1
Field ZIB_1 = new Field("ZIB1");
// Create Field ZIB_5
Field ZIB_5 = new Field("ZIB5");
 
// Create Component ZIB.5.2
Component com1 = new Component("ZIB.5.2");
 
// Add Component ZIB.5.2 to Field ZIB_5
// 2nd parameter here specifies the component position, for inserting segment on particular position
// If we don’t provide 2nd parameter, component will be inserted to next position (if field has 2 components this will be 3rd, 
// If field is empty this will be 1st component
ZIB_5.AddNewComponent(com1, 2);
 
// Add Field ZIB_1 to segment ZIB, this will add a new filed to next field location, in this case first field
newSeg.AddNewField(ZIB_1);
 
// Add Field ZIB_5 to segment ZIB, this will add a new filed as 5th field of segment
newSeg.AddNewField(ZIB_5, 5);
 
// Add segment ZIB to message
bool success = message.AddNewSegment(newSeg);
````

New Segment would look like this:

````text
ZIB|ZIB1||||ZIB5^ZIB.5.2
````

After evaluated and modified required values, the message can be obtained again in text format

````cSharp
string strUpdatedMsg = message.SerializeMessage();
````

### Remove Trailing Components

```csharp
var message = new Message();

// create ORC segment
var orcSegment = new Segment("ORC", new HL7Encoding());

// add fields
for (int eachField = 1; eachField <= 12; eachField++)
{
    orcSegment.AddEmptyField();
}

// add components to field 12
for (int eachField = 1; eachField < 8; eachField++)
{
    orcSegment.Fields(12).AddNewComponent(new Component(new HL7Encoding()));
}

// add values to components
orcSegment.Fields(12).Components(1).Value = "should not be removed";
orcSegment.Fields(12).Components(2).Value = "should not be removed";
orcSegment.Fields(12).Components(3).Value = "should not be removed";
orcSegment.Fields(12).Components(4).Value = ""; // should not be removed because in between valid values
orcSegment.Fields(12).Components(5).Value = "should not be removed";
orcSegment.Fields(12).Components(6).Value = ""; // should be removed because trailing
orcSegment.Fields(12).Components(7).Value = ""; // should be removed because trailing
orcSegment.Fields(12).Components(8).Value = ""; // should be removed because trailing

orcSegment.Fields(12).RemoveEmptyTrailingComponents();
message.AddNewSegment(orcSegment);

string serializedMessage = message.SerializeMessage(false);
```

### Remove a Segment

Segments are removed individually, including the case where there are repeated segments with the same name

````cSharp
    // Remove the first segment with name NK1
    bool success = message.RemoveSegment("NK1") 

    // Remove the second segment with name NK1
    bool success = message.RemoveSegment("NK1", 1) 
````

### Encoded segments

Some contents may contain forbidden characters like pipes and ampersands. Whenever there is a possibility of having those characters, the content shall be encoded before calling the 'AddNew' methods, like in the following code:

````cSharp
var obx = new Segment("OBX", new HL7Encoding());

// Not encoded. Will be split into parts.
obx.AddNewField("70030^Radiologic Exam, Eye, Detection, FB^CDIRadCodes");  

// Encoded. Won't be parsed nor split.
obx.AddNewField(obx.Encoding.Encode("domain.com/resource.html?Action=1&ID=2"));  
````

### Copying a segment

The DeepCopy method allows to perform a clone of a segment when building new messages. Countersense, if a segment is referenced directly when adding segments to a message, a change in the segment will affect both the origin and new messages.

````cSharp
Segment pid = ormMessage.DefaultSegment("PID").DeepCopy();
oru.AddNewSegment(pid);
````

### Null elements

Null elements (fields, components or subcomponents), also referred to as Present But Null, are expressed in HL7 messages as double quotes, like (see last field):

````
EVN|A04|20110613083617||""
````

Whenever requested individually, those elements are returned as `null`, rather than double quotes:

````cSharp
var expectEmpty = evn.Fields(3).Value; // Will return an empty string
var expectNull = evn.Fields(4).Value; // Will return null
````

### Date Handling

A couple of date handling methods have been added, for parsing elements containing valid date/times, including time zones, as described in [the HL7 standard](http://www.hl7.eu/refactored/dtDTM.html). Examples:

````csharp
// With time zone
string value1 = "20151231234500.1234+2358";
TimeSpan offset;
DateTime? dt1 = MessageHelper.ParseDateTime(value1, out offset);

// Date/time only
string value2 = "20151231234500";
DateTime? dt2 = MessageHelper.ParseDateTime(value2);
````

`ParseDateTime` will catch exceptions by default and return `null` in case of invalid dates. For preventing this mechanism, add an extra argument as `true`, like:

````csharp
try
{
    var dt1 = MessageHelper.ParseDateTime(value1, out TimeSpan offse, true);
    var dt2 = MessageHelper.ParseDateTime(value2, true);
}
catch 
{
    // do something here
}
````

## Credits
This is a fork from Jayant Singh's HL7 parser. Since then, it has been modified fundamentally, with respect to features, code quality, bugs and typos. 
For more information about the original implementation read:
- https://github.com/j4jayant/hl7-cSharp-parser
- http://j4jayant.com/articles/hl7/31-hl7-parsing-lib

The field encoding and decoding methods have been based on https://github.com/elomagic/hl7inspector

## Breaking changes
Since version 2.9, the MSH segment will have an extra field at the beginning of the segment list, containing the field separator. This is according to [the HL7 standard]( https://www.hl7.org/documentcenter/public_temp_CACD15D9-1C23-BA17-0C050D19F5A35765/wg/conf/HL7MSH.htm), as mentioned in Issue #26. Every field index in that segment should be increased by one.

Since version 2.9, some previously deprecated methods starting with lowercase have been removed. The replacement methods starting with uppercase shall be used instead.

Since version 2.21, message.GetValue() will decode the returned content.
