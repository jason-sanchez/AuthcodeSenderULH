Imports System
Imports System.Threading
Imports System.Net
Imports System.Net.Sockets
Imports System.IO
Imports System.IO.File
Imports System.Configuration
Imports System.Text
Imports System.Data
Imports System.Data.SqlClient
Imports System.Globalization
Imports Ionic.Zip



Module Module1
    Dim NetStream As NetworkStream
    Dim client As New TcpClient
    Dim filewriter As System.IO.StreamWriter
    'Public objIniFile As New INIFile("d:\W3Production\HL7Transmitter.ini") '20140818
    Dim IPAddress As String = System.Configuration.ConfigurationManager.AppSettings("IPAddress")
    Dim port As String = System.Configuration.ConfigurationManager.AppSettings("Port")
    Dim AuthCodeDirectory As String = System.Configuration.ConfigurationManager.AppSettings("AuthCodeDirectory")
    Dim AuthLogFile As String = System.Configuration.ConfigurationManager.AppSettings("AuthCodeLog")
    Dim AuthFile As String = System.Configuration.ConfigurationManager.AppSettings("AuthFile")
    Dim errorfilepath As String = System.Configuration.ConfigurationManager.AppSettings("errorlog")
    Dim history As String = System.Configuration.ConfigurationManager.AppSettings("authcodeHistory")
    Dim problemsDir As String = System.Configuration.ConfigurationManager.AppSettings("problems")
    Dim countA As String = System.Configuration.ConfigurationManager.AppSettings("countA")
    Dim errorfilename As String = String.Format("CopyError_{0:yyyyMMdd_HH-mm-ss}.txt", Date.Now)

    Sub Main()
        Dim auth = My.Computer.FileSystem.GetFiles(AuthCodeDirectory)

        If auth.Count > 0 Then
            Try

                filewriter = My.Computer.FileSystem.OpenTextFileWriter(AuthLogFile, True)
                'IPAddress =
                'port =
                'AuthCodeDirectory = 
                'start connection
                IPAddress = IPAddress
                port = port
                client.Connect(IPAddress, port)

                System.Windows.Forms.Application.DoEvents()

                If client.Connected Then
                    filewriter.WriteLine("----------------------------------------------------")
                    filewriter.WriteLine("Server: " & IPAddress & ":" & port & " connected.")
                    filewriter.WriteLine("----------------------------------------------------" & vbCrLf)
                    Console.WriteLine("Server: " & IPAddress & ":" & port & " connected.")

                    System.Windows.Forms.Application.DoEvents()
                    copyFiles()
                    SendFiles()
                End If

            Catch ex As Exception
                Dim errorfile = New StreamWriter(errorfilepath & errorfilename, True)
                errorfile.Write("Main Error" & ex.Message & vbCrLf)
                errorfile.Close()
                Exit Sub
            Finally
                filewriter.Close()
            End Try
        End If
    End Sub

    Private Sub SendFiles()

        Try

            Dim filename As String = ""
            Dim dir As String = ""
            NetStream = client.GetStream()
            Dim bytesToSend(client.SendBufferSize) As Byte

            Dim authdirectory As String() = Directory.GetFiles(AuthCodeDirectory, AuthFile)

            Dim counter As Integer = 0

            For Each filename In authdirectory
                counter = counter + 1
                'Only send one at a time
                If counter > countA Then
                    Exit For
                End If

                Dim theFile As New FileInfo(filename)
                Dim fileStr As New FileStream(filename, FileMode.Open, FileAccess.Read)
                Dim fileReader As New BinaryReader(fileStr)
                Dim numBytesRead As Integer = 0
                Dim i As Integer = 0
                Do Until i >= theFile.Length - 1
                    numBytesRead = fileStr.Read(bytesToSend, 0, bytesToSend.Length)
                    NetStream.Write(bytesToSend, 0, numBytesRead)
                    i = i + numBytesRead
                    NetStream.Flush()
                Loop
                'NetStream.Flush()
                filewriter.WriteLine(filename & " Sent.")
                ' Receive the TcpServer.response.
                ' Buffer to store the response bytes.
                Dim Data = New [Byte](256) {}
                ' String to store the response ASCII representation.
                Dim responseData As [String] = [String].Empty
                ' Read the first batch of the TcpServer response bytes.
                Dim bytes As Int32 = NetStream.Read(Data, 0, Data.Length)

                responseData = System.Text.Encoding.ASCII.GetString(Data, 0, bytes)
                'check to see if we got a positive response. Looking for MSA|AA|
                If InStr(responseData, "MSA|AA|") <> 0 Then
                    'file.WriteLine("======================================== " & vbCrLf)
                    filewriter.WriteLine("Received: " & responseData & vbCrLf)

                    '20121108 - send complete. delete the file but close the filestream first because it is using f1.
                    'Application.DoEvents()
                    fileStr.Close()
                    theFile.Delete()

                Else
                    'did not receive a positice acknowledgement so save it in the problems directory
                    filewriter.WriteLine(filename & " not acknowledged for. Copied to problems directory." & vbCrLf)

                    'Application.DoEvents()

                    'first, close the file stream to release the file so we can work with it.
                    fileStr.Close()
                    Dim fi2 As FileInfo = New FileInfo(problemsDir & theFile.Name)
                    fi2.Delete()
                    theFile.CopyTo(problemsDir & theFile.Name)
                    theFile.Delete()
                End If
            Next
        Catch ex As Exception
            Dim errorfile = New StreamWriter(errorfilepath & errorfilename, True)
            errorfile.Write("Send Error" & ex.Message & vbCrLf)
            errorfile.Close()
            filewriter.WriteLine(ex.Message & vbCrLf)
            Exit Sub
        End Try
    End Sub

    Private Sub copyFiles()

        Dim authdirectory As String() = Directory.GetFiles(AuthCodeDirectory, AuthFile)
        Try
            Dim counter As Integer = 0
            For Each file1 As String In Directory.GetFiles(AuthCodeDirectory, "HL7.*")
                counter = counter + 1
                'Only copy one at a time
                If counter > countA Then
                    Exit For
                End If
                Dim theFile2 As New FileInfo(file1)
                Dim saveCopy As String = history & theFile2.Name
                File.Copy(file1, saveCopy)
            Next
        Catch ex As Exception
            Dim errorfile = New StreamWriter(errorfilepath & errorfilename, True)
            errorfile.Write("Copy Error" & ex.Message & vbCrLf)
            errorfile.Close()
            Exit Sub
        End Try
    End Sub

End Module
