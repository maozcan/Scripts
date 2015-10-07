'Export Email: 
Option Public 
Dim CONVERT_DB_SERVER As String 
Dim CONVERT_DB_NAME As String 
Dim CONVERT_FORM As String 
Dim CONVERT_FIELD As String 
Dim CONVERT_TOFIELD As String 
Dim OUTFILENAME As String 
Dim crlf As String 
Dim SaveTempDoc As Integer 
Dim fileNum As Integer 
Dim doc As NotesDocument 
Dim nstream As NotesStream 
Dim x As String 
Dim count As Integer 
Dim b As String 


'** ShellExecute will open a file using the registered file association on the computer. 

'** If it returns a value of greater than 32 then the call was successful; otherwise 

'** it should return one of the error codes below. The parameters are: 

'**hwnd = an active window handle, or 0 

'**operation = "edit", "explore", "find", "open", or "print" 

'**fileName = a file or directory name 

'**parameters = if fileName is an executable file, the command line parameters 

'**to pass when launching the application, or "" if no parameters 

'**are necessary 

'**directory = the default directory to use, or "" if you don't care 

'**displayType = one of the displayType constants listed below 

Declare Function ShellExecute Lib "shell32" Alias "ShellExecuteA" _ (Byval hwnd As Long, Byval operation As String, Byval fileName As String, _ Byval parameters As String, Byval directory As String, Byval displayType As Long) As Long 


'** FindExecutable will determine the executable file that is set up to open a particular 

'** file based on the file associations on this computer. If it returns a value of greater than 

'** 32 then the call was successful; otherwise it should return one of t he error codes 

'** below. The parameters are: 

'**fileName = the full path to the file you are trying to find the association for 

'**directory = the default directory to use, or "" if you don't care 

'**retAssociation = the associated executable will be returned as this parameter, 

'**with a maximum string length of 255 characters (you will want 

'**to pass a String that's 256 characters long and trim the 

'**null-terminated result
) 

Declare Function FindExecutable Lib "shell32" Alias "FindExecutableA" _ (Byval fileName As String, Byval directory As String, Byval retAssociation As String) As Long 

'** constants for the displayType parameter 
Const SW_HIDE = 0 
Const SW_SHOWNORMAL = 1 
Const SW_NORMAL = 1 
Const SW_SHOWMINIMIZED = 2 
Const SW_SHOWMAXIMIZED = 3 
Const SW_MAXIMIZE = 3 
Const SW_SHOWNOACTIVATE = 4 
Const SW_SHOW = 5 
Const SW_MINIMIZE = 6 
Const SW_ SHOWMINNOACTIVE = 7 
Const SW_ SHOWNA = 8 
Const SW_RESTORE = 9 
Const SW_SHOWDEFAULT = 10 
Const SW_MAX = 10 

'** possible errors returned by ShellExecute 
Const ERROR_OUT_OF_MEMORY = 0 'The operating system is out of memory or resources. 
Const ERROR_FILE_NOT_FOUND = 2 'The specified file was not found. 
Const ERROR_PATH_NOT_FOUND = 3 'The specified path was not found. 
Const ERROR_BAD_FORMAT = 11 'The .exe file is invalid (non-Microsoft Win32® .exe or error in .exe image). 
Const SE_ERR_FNF = 2 'The specified file was not found. 
Const SE_ERR_PNF = 3 'The specified path was not found. 
Const SE_ERR_ACCESSDENIED = 5 'The operating system denied access to the specified file. 
Const SE_ERR_OOM = 8 'There was not enough memory to complete the operation. 
Const SE_ERR_SHARE = 26 'A sharing violation occurred. 
Const SE_ERR_ASSOCINCOMPLETE = 27 'The file name association is incomplete or invalid. 
Const SE_ERR_DDETIMEOUT = 28 'The DDE transaction could not be completed because the request timed out. 
Const SE_ERR_DDEFAIL = 29 'The DDE transaction failed. 
Const SE_ERR_DDEBUSY = 30 'The Dynamic Data Exchange (DDE) transaction could not be completed because other DDE transactions were being processed. 
Const SE_ERR_NOASSOC = 31 'There is no application associated with the given file name extension. This error will also be returned if you attempt to print a file that is not printable. 
Const SE_ERR_DLLNOTFOUND = 32 'The specified dynamic-link library (DLL) was not found. 

Declare Function GetActiveWindow Lib "user32.dll" () As Long 
' // BrowseInfo stucture 
Type BROWSEINFO 
	hwndOwner As Long 
	pidlRoot As Long 
	pszDisplayName As String 
	lpszTitle As String 
	ulFlags As Long lpfn 
	As Long lParam As Long 
	iImage As Long 
End Type 
' // BrowseFlags constants 
Const BIF_BROWSEFORCOMPUTER = 1000 
Const BIF_BROWSEFORPRINTER = 2000 
Const BIF_DONTGOBELOWDOMAIN = 2 
Const BIF_RETURNFSANCESTORS = 8 
Const BIF_RETURNONLYFSDIRS = 1 
Const BIF_STATUSTEXT = 4 
Const MAX_SIZE = 255 
' // Win32 function to browse for a folder, rather than a file or files 
Declare Function BrowseFolderDlg Lib "shell32.dll" Alias "SHBrowseForFolder" (lpBrowseInfo As BROWSEINFO) As Long 
' // Win32 function that returns the path of the folder selected 
Declare Function GetPathFromIDList Lib "shell32.dll" Alias "SHGetPathFromIDList" (Byval PointerToIDList As Long, Byval pszPath As String) As Long 

Sub Initialize 
Dim s As New NotesSession 
Dim db As NotesDatabase 
Dim dc As NotesDocumentCollection 
Dim body As NotesItem 
Dim rtitem As NotesRichTextItem 
Dim mimebits As Variant 
Dim n As Integer 
Dim msgid As Variant 

crlf = Chr(13) & Chr(10) 'Dim mime As NotesMIMEEntity, mime2 As NotesMIMEEntity 

'** this is a form that has a rich text field that is set to store contents 

'** in MIME format CONVERT_FORM = "MimeConvert" 

'** this is the field on the form mentioned above that stores rich text 

'** as MIME CONVERT_TOFIELD="MimeRichTextField" CONVERT_FIELD = "Body" 

'** do you want to save the temporary doc after you're done with it 

'** (True) or delete it (False)? 
SaveTempDoc = False 
expdir$=BrowseForFolder() 
If expdir$="" Then 
Messagebox "You have not selected a directory", MB_OK, "Select output Directory" 
Exit Sub 
End If 
Dim mime As NotesMIMEEntity 
Dim subj As String 
Set nstream=s.CreateStream 
Set db = s.CurrentDatabase 
s.ConvertMime = False ' Do not convert MIME to rich text| 
Set dc = db.UnprocessedDocuments 
Set doc = dc.GetFirstDocument 
n=0 
While Not(doc Is Nothing) 
n=n+1 
If doc.subject(0) ="" 
Then subj="No subject" 
Else subj=validatefilename(doc.subject(0)) 
End If OUTFILENAME=expdir$ & "" & subj & " - " & doc.NoteID & ".eml" 
Set body = doc.GetFirstItem("Body") 
fileNum% = Freefile 
fileName$ = OUTFILENAME 
Open filename$ For Output As fileNum% 
If body.Type = MIME_PART 
Then Set mime = body.GetMimeEntity 
mimebits=getmultipartmime(mime) 
Print #fileNum%, mimebits 
Else Call GetRichTextAsHtmlFile (doc, CONVERT_FIELD, OUTFILENAME, True) 
End If 
Close fileNum% 'Kill filename$ 
Set doc = dc.GetNextDocument(doc) 
Wend Msgbox Cstr(n) & " emails have been exported to " & expdir$ 
End Sub 
Function remsub(substr As String) 
Dim mystr As String 
For a=1 To Len(substr) 
y=Asc(Mid$(substr,a,1)) 
If Not ( y="13" Or y="10") Then 
mystr=mystr+Mid$(substr,a,1) 
End If 
Next remsub=mystr 
End Function 

Function GetBoundary (header As String) As String 

'** get the boundary from the initial header of a multi-part MIME string 

'** normally, the format in Notes is something like: 

'** Content-Type: multipart/related; boundary="=_related 0012868C85256E16_=" 
Dim boundary As String 
boundary = Strright(header, "boundary=""") 

'** we want everything from the boundary=" to the closing " 
If (Instr(boundary, """") > 0) Then 
boundary = Strleft(boundary, """") 
End If 
If (Len(boundary) > 0) Then 
boundary = "--" & boundary 
End If 
GetBoundary = boundary 
End Function 
Function GetMultipartMime (mime As NotesMIMEEntity) As String 

'** recursively get all the parts of a multi-part MIME entity 
Dim child As NotesMIMEEntity 
Dim mText As String 
Dim boundary As String 
count=count+1 
boundary = GetBoundary(mime.Headers) 

'** DANGER -- ContentAsText truncates large MIME bodies in R5!!! 

'** ND6 seems to be okay... 
If mime.ContentType<>"text" Then 
Call mime.encodecontent(1727) mText = mText & mime.Headers & crlf & crlf mText = mText & mime.ContentAsText & crlf 
Else 
mText = mText & mime.Headers & crlf & crlf mText = mText & crlf & mime.ContentAsText & crlf 
End If 
Set child = mime.GetFirstChildEntity 
While Not(child Is Nothing) 
mText = mText & boundary & crlf mText = mText & GetMultipartMime(child) 
Set child = child.GetNextSibling 
Wend 
If (Len(boundary) > 0) Then 
mText = mText & boundary & "--" & crlf & crlf 
End If 
GetMultipartMime = mText 
End Function 
Function getlist(field As String) 
Dim values As Variant 
Dim out As String 
Dim session As New NotesSession 
Dim nam As NotesName 
values = doc.GetItemValue( field ) 
Forall v In values c=c+1 
Set nam=session.CreateName(v) 
If c>1 Then 
out = out +"; "+ nam.abbreviated 
Else 
out=nam.abbreviated 
End If 
End Forall 
getlist=out 
End Function 
Function WriteHtmlStringToFile (htmlBody As String, _ fileName As String, setFileExtension As Integer, isMultiPart As Integer) As Integer 

'** send a NotesStream containing HTML to the specified fileName 

'** (if setFileExtension is True, the fileName will automatically have 

'** either .htm or .mht appended as the file extension, depending 

'** on whether isMultiPart is True (.mht) or False (.htm)) 
Dim htmlStart As String, htmlEnd As String 

'** set our variables, based on isMultiPart and setFileExtension If Not isMultiPart Then 

'** non-multi-part files need opening and closing HTML 
htmlStart = "<html><body>" 
htmlEnd = "</body></html>" 
End If 'fileName = fileName & ".eml" 
'** open the file for output 'fileNum = Freefile() 'Open fileName For Output As fileNum Print #fileNum%,"From: " & getlist("From") Print #fileNum%,"To: " & getlist("SendTo") Print #fileNum%,"Cc: " & getlist("CopyTo") Print #fileNum%, "Bcc: " & getlist("BlindCopyTo") Print #fileNum%,"Subject: " & doc.subject(0) Print #fileNum%, "Date: " & Format(doc.posteddate(0), "dd mmm yyyy hh:mm:ss") msgid=doc.GetItemValue("$MessageID") Print #fileNum, "Message-ID: " & msgid(0) If Not ismultipart Then Print #fileNum%, "MIME-Version: 1.0" If Not ismultipart Then Print #fileNum%,"Content-Type: multipart/alternative;" If Not ismultipart Then Print #fileNum%, Chr(09) & |boundary="| & Cstr(doc.NoteID) & |"| Print #1, "X-Priority: " & doc.importance(0) 
Forall i In doc.Items 
If i.text<>"" Then If i.name<>"Body" Then 
Print #1, "X-Notes-Item: " & i.text & "; name=" & i.name 
End If 
End If 
End Forall 
If Not ismultipart Then 
Print #fileNum%, crlf & "--" & Cstr(doc.NoteID) 
If Not ismultipart Then 
Print #fileNum%,"Content-Type: text/html;" If Not ismultipart Then Print #fileNum%, Chr(09) & |charset="iso-8859-1"| 
If Not ismultipart Then 
Print #fileNum%, "Content- Transfer-Encoding: quoted-printable" & crlf 
If Not ismultipart Then 
Print #fileNum%, htmlStart Print #fileNum%, htmlBody 
If Not ismultipart Then 
Print #fileNum%, htmlEnd & crlf If Not ismultpart Then Print #fileNum%, crlf & "--" & Cstr(doc.NoteID) & "--" 
'Close #fileNum 
WriteHtmlStringToFile = True 
Exit Function processError: Print "Error " & Err & ": " & Error$ Reset WriteHtmlStringToFile = False 
Exit Function 
End Function 
Function RefreshDocFields (doc As NotesDocument) As String 

'** Refresh the fields on a document, and return the NoteID of 
'** the refreshed doc (I don't think this would cause the NoteID 
'** to change, but just in case) On Error Resume Next 
'** before we save the uidoc, disable any MIME conversion warnings 
'** by setting the MIMEConvertWarning parameter in Notes.ini to 1 
Dim session As New NotesSession 
Dim oldWarningVal As String 
oldWarningVal = session.GetEnvironmentString ("MIMEConvertWarning", True) 
Call session.SetEnvironmentVar ("MIMEConvertWarning", "1", True) 
Dim workspace As New NotesUIWorkspace 
Dim uidoc As NotesUIDocument Set uidoc = workspace.EditDocument(True, doc) 
Call uidoc.Save RefreshDocFields = uidoc.Document.NoteID Call uidoc.Close(True) 
%REM 
'** if you're not running this on a Notes client, you could 
'** try to run this in the background by doing everything 
'** using the Notes COM objects, although this is totally 
'** unsupported and probably riddled with memory leaks 
'** if you could actually get it working (plus, it would only 
'** work on a Windows server...) 
Dim oleSession As Variant 
Dim oleDb As Variant 
Dim oleDoc As Variant 
Dim oleWorkspace As Variant 
Dim oleUidoc As Variant 
'** first we have to get a handle to the doc as an OLE object 
Set oleSession = CreateObject("Notes.NotesSession") 
Call oleSession.Initialize 
Set oleDb = oleSession. GetDatabase("", doc.ParentDatabase.FilePath) 
Set oleDoc = oleDb.GetDocumentByID(doc.NoteID) 
'** if we were able to do that, we can open and save it as a UIDoc 
'** using COM 
If Not (oleDoc Is Nothing) Then 
Set oleWorkspace = CreateObject("Notes.NotesUIWorkspace") 
Set oleUidoc = oleWorkspace.EditDocument(True, oleDoc) 
Call oleUidoc.Save RefreshDocFields = oleUidoc.Document.NoteID 
Call oleUidoc.Close(True) 
End If 
%END REM 
'** reset the MIMEConvertWarning Notes.ini variable and return 
Call session.SetEnvironmentVar ("MIMEConvertWarning", oldWarningVal, True) 
End Function 
Function GetRichTextAsHtmlFile (doc As NotesDocument, _ fieldName As String, fileName As String, setFileExtension As Integer) As Integer 
'** convert a rich text field to HTML, and send it to the specified file 
'** (if setFileExtension is True, the fileName will automatically have 
'** either .htm or .mht appended as the file extension, depending 
'** on whether the HTML representation is multi-part or not) 
Dim isMultiPart As Integer 
Dim htmlBody As String 
htmlBody = GetRichTextAsHtmlString (doc, fieldName, isMultiPart) 
GetRichTextAsHtmlFile = WriteHtmlStringToFile (htmlBody, fileName, True, isMultiPart) 
End Function 
Function GetRichTextAsHtmlString (doc As NotesDocument, _ fieldName As String, isMultiPart As Integer) As String 
'** get the contents of the given field as HTML by copying them 
'** to a MIME rich text field and reading the MIME field 
Dim session As New NotesSession 
Dim mText As String 
Dim db As NotesDatabase 
Dim newDoc As NotesDocument 
Dim noteID As String 
Dim currentSessionMimeSetting As Integer 
Dim rtitem As NotesRichTextItem 
Dim rtitem2 As NotesRichTextItem 
Dim mimeItem As NotesItem 
Dim mime As NotesMIMEEntity 
Dim MimeFieldName As String 
'** make sure we can actually get the rich text field we want to 
'** copy, and make sure it's really rich text (error 13 if it's not) 
On Error 13 Resume Next 
Set rtitem = doc.GetFirstItem(fieldName) 
If (rtitem Is Nothing) Then 
Exit Function 
End If 

'** save the current ConvertMime setting, because we'll change it 

'** a couple of times 
currentSessionMimeSetting = session.ConvertMime 

'** initially set the ConvertMime property to True and create a 
'** temporary document, which allows us to treat the MIME field 
'** as rich text so we can append some real rich text to it 
session.ConvertMime = True 
'** create a new document to manipulate the MIME entry with. 
Set db =session.CurrentDatabase 'Set db = session. GetDatabase(CONVERT_DB_SERVER, CONVERT_DB_NAME) 
Set newDoc = New NotesDocument(db) 
'** this document must use a form that already exists in this 
'** database, and the MIME field that we create must be the 
'** same name as a field that's already on the form as a rich text 
'** field that stores its data in MIME format 
newDoc.Form = CONVERT_FORM 
MimeFieldName = CONVERT_TOFIELD 
Set rtitem2 = New NotesRichTextItem (newDoc, MimeFieldName) 
Call rtitem2.AppendRTItem(rtitem) 
Call newDoc.Save(True, True) 
'** HERE'S THE TRICK: you have to open the temporary doc 
'** as a uidoc, and then save and close it. 
'** This will convert all the rich text in our MIME field back to 
'** MIME format (which is why the field had to exist as a valid 
'** MIME field on a valid form in the first place, so Notes will 
'** know to convert it back) 
noteID = RefreshDocFields(newDoc) 
'** after you've done this, you need to reset the reference for 
'** the newDoc variable, so none of the in-memory information 
'** about the document will remain 
Set newDoc = Nothing 
'** set ConvertMime to False, reopen the temporary doc, 
'** and now we can get the rich text contents as HTML 
session.ConvertMime = False 
Set newDoc = db.GetDocumentByID(noteID) 
Set mimeItem = newDoc. GetFirstItem(MimeFieldName) 
If Not (mimeItem Is Nothing) Then 
If (mimeItem.Type = MIME_PART) Then 
Set mime = mimeItem.GetMimeEntity 
If Not (mime Is Nothing) Then 
If (mime.ContentType = "multipart") Then 
'** for multi-part MIME, which is anything with graphics, 
'** you need to get the various parts one at a time. 
'** If you write this to a file, it should be a .mht file so the 
'** the browser knows what to do with it. 
'** NOTE: there is a bug in R5 where you can't always 
'** get the full contents of large sections of multi-part 
'** MIME -- if you're dealing with large images, they will 
'** often get cropped off at the bottom 
isMultipart = True 
mText = GetMultipartMime(mime) 
Else 
'** if we're not dealing with multi-part (thank goodness) 
'** we can just grab the HTML contents and go 
isMultipart = False 
mText = mText & mime.ContentAsText 
End If 
End If 
End If 
End If 
'** delete or save the temporary doc when we're done (depending on 
'** the SaveTempDoc setting) 
If SaveTempDoc Then 
Set rtitem2 = New NotesRichTextItem (newDoc, "HTMLText") 
Call rtitem2.AppendText(mText) 
Call newDoc.Save(True, True) 
Else 
Call newDoc.Remove(True) 
End If 
'** set the ConvertMIME setting back to whatever it was 
'** before we started all this, and exit out 
session.ConvertMIME = currentSessionMimeSetting 
GetRichTextAsHtmlString = mText 
End Function 
Function validatefilename(filename As String) 
Dim l As Integer 
Dim x As Integer 
Dim newname As String 
l=Len(filename) 
For x = 1 To l 
If Mid$(filename,x,1) Like "[-@()~^$#[{}=A-Za-z0-9]" Then 
newname=newname+Mid$(filename,x,1) 
Else 
If Mid$(filename,x,1)=" " Or Mid$(filename,x,1)="]" Or Mid$(filename,x,1)= "," Or Mid$(filename,x,1)="'" Or Mid$(filename,x,1)="!" Then 
newname=newname+Mid$(filename,x,1) 
Else 
Print Mid$(filename,x,1) " is not valid" 
End If 
End If 
Next x 
validatefilename=newname 
End Function 
Function isFolder(Byval sFolderPath As String) As Integer 
Const ATTR_DIRECTORY = 16 
isFolder = False 
If Dir$(sFolderPath, ATTR_DIRECTORY) <> "" Then 
isFolder = True 
End Function 
Function isFile(Byval sFileName As String) As Integer 
On Error Resume Next 
Dim lFileLength As Long 
Const ATTR_NORMAL = 0 isFile = False 
If Dir$(sFileName, ATTR_NORMAL) <> "" Then lFileLength = Filelen(sFileName) 
If (lFileLength > 0) Then isFile = True 
End If 
End Function 
Function BrowseForFolder() As String 
Dim mBrowseInfo As BROWSEINFO 
Dim lngPointerToIDList As Long 
Dim lngResult As Long 
Dim strPathBuffer As String 
Dim strReturnPath As String 
Dim vbNullChar As String 
vbNullChar = Chr(0) 
On Error Goto lblErrs 
mBrowseInfo.hwndOwner = GetActiveWindow() 
' // Set the default folder for the dialog box (0 = My Computer, ' // 5 = My Documents) 
mBrowseInfo.pidlRoot = 0 
mBrowseInfo.lpszTitle = "Select the folder you wish to use:" 
' // Pointer to a buffer that receives the display name 
' // of the folder selected by the user 
mBrowseInfo.pszDisplayName = String(MAX_SIZE, Chr(0)) 
' // Value specifying the types of folders to be listed 
' // in the dialog box as well as other options 
mBrowseInfo.ulFlags = BIF_RETURNONLYFSDIRS 
' // Returns a pointer to an item identifier list that 
' // specifies the location of the selected folder relative 
' // to the root of the name space 
lngPointerToIDList = BrowseFolderDlg(mBrowseInfo) 
If lngPointerToIDList <> 0& Then 
' // Create a buffer 
strPathBuffer = String(MAX_SIZE, Chr(0)) 
' // Now get the selected path 
lngResult = GetPathFromIDList (Byval lngPointerToIDList, Byval strPathBuffer) 
' // And return just that 
strReturnPath = Left$(strPathBuffer, Instr(strPathBuffer, vbNullChar) - 1) 
End If 
BrowseForFolder = strReturnPath 
lblEnd: 
Exit Function 
lblErrs: 
Messagebox "Unexpected error: " & Error$ & " (" & Cstr(Err) & ").", 0, "Error" 
Resume lblEnd 
End Function 
Sub Terminate 
End Sub
