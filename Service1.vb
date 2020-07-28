Imports System.IO
Imports System.Timers
Imports System.Data.SqlClient
Imports System.Net
Imports System.Configuration

Public Class Service1

    Private WithEvents Timer1 As Timer
    Private WithEvents Timer2 As Timer
    Dim x As Integer = 0
    Dim dote As Boolean = True
    Dim Config As Configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)

#Region "連結DB的設定"
    Function GetConnSetting() As String

        Dim DB_IP As String = ConfigurationManager.AppSettings("DB_IP")

        'Return "Data Source=192.168.11.29;Initial Catalog= HL_ELearning_Doc;User ID=hladmin;Password=19763180;"
        Return DB_IP
    End Function
#End Region

#Region "啟動、關閉服務"
    Protected Overrides Sub OnStart(ByVal args() As String)
        'MsgBox("MessageBox Show Test!", 100)
        '設定定時器屬性

        Timer1 = New Timer With {
                .Interval = 1000 * 60   '設定60秒為間隔
            }
        Timer2 = New Timer With {
                 .Interval = 1000  '設定60秒為間隔
            }

        '啟動定時器
        Timer1.Start()

    End Sub

    '在此加入啟動服務的程式碼。這個方法必須設定已啟動的
    '事項， 否則可能導致服務無法工作。

    Protected Overrides Sub OnStop()
        '在此加入停止服務所需執行的終止程式碼。
        Try

        Catch ex As Exception

        End Try
    End Sub

#End Region

#Region "Timer1 計時器，觸發click事件"
    Private Sub Timer1_Tick(sender As Object, e As System.Timers.ElapsedEventArgs) Handles Timer1.Elapsed
        'Dim Get_Time As String = Config.AppSettings.Settings("Get_Time").Value
        'Dim Put_Time As String = Config.AppSettings.Settings("Put_Time").Value

        Try
            Using Conn As SqlConnection = New SqlConnection(GetConnSetting()) 'DB連結(搭建DB跟伺服器的橋樑)
                Conn.Open()
                Using Cmd As SqlCommand = New SqlCommand

                    Cmd.Connection = Conn

                    If dote Then
                        M3U8_Create(Cmd) '播放列表製作
                    End If

                End Using
            End Using
        Catch ex As Exception
            Insert_Log(ex.Message)
        End Try
    End Sub
#End Region

#Region "Timer2 計時器，觸發click事件"
    Private Sub Timer2_Tick(sender As Object, e As System.Timers.ElapsedEventArgs) Handles Timer2.Elapsed
        x = x + 1
    End Sub
#End Region



#Region "播放列表製作"
    Sub M3U8_Create(ByVal Cmd As SqlCommand)
        '從資料表中獲取 code + number 作為 filename，提供之後要組成FFMPEG指令碼的字串中的input output 名稱,路徑

        Dim SQL As String = ""
        Dim filename As String = ""
        Dim KVL As String = ""
        Dim KCL As String = ""
        Dim VCR As String = ""
        Dim Have_Data As Boolean = False
        Dim TTS As String = ""
        x = 0
        SQL &= "  Declare  @dote varchar(100) =(Select FFMPEG_PATH FROM EL_mp4_setting)   "
        SQL &= " Select "
        SQL &= "   top 1 "
        SQL &= "   @dote  as 'FFMPEG_PATH',Path As 'filename',  "
        SQL &= "   Replace(Path, 'mp4', 'm3u8') as 'm3u8path', "
        SQL &= "   Number, code "
        SQL &= " From EL_MP4 "
        SQL &= " Where Complete = 'N' "

        Cmd.CommandText = SQL

        Using DataReader As SqlDataReader = Cmd.ExecuteReader
            If DataReader.HasRows Then '檢查從資料庫拉回的資料是否，不為空值
                Have_Data = True
                dote = False
                DataReader.Read()
                filename = DataReader("filename").ToString() 'filename = number + code 影片位置
                KVL = DataReader("m3u8path").ToString() 'm3u8path = REPLACE (Path, 'mp4', 'm3u8') 輸出位置
                KCL = DataReader("code").ToString()
                VCR = DataReader("number").ToString()
                TTS = DataReader("FFMPEG_PATH").ToString()

            Else
                filename = Nothing
            End If
        End Using

        '此區域無用 62-75
        'Dim appPath As String = "C:\allen0916" 'appPath是指向實體路徑，
        'Dim Name As String = "hlad" & sum
        'Dim saveDir As String = "hlad" & sum

        'Dim savePath As String = appPath & saveDir
        'Dim saveResult As String = savePath & "\" & Name & ".mp4"

        'Dim m3u8Path As String = Path.Combine(savePath, Name + ".m3u8")
        'context.Response.Write(savePath)
        'context.Response.Write(m3u8Path)
        'context.Response.End()

        '此區域無用

        Dim para As String = String.Format(" -i {0} -profile:v baseline -level 3.0 -s 1920x1080 -start_number 0 -hls_time 10 -hls_list_size 0 -f hls {1} ", filename, KVL)
        'MsgBox(saveResult)
        Insert_Log(KVL)
        'MsgBox(para)
        Timer2.Start()
        Dim output As String = "none" '可以輸出output查看具體報錯原因

        Dim p As Process = New Process()
        Dim pinfo As ProcessStartInfo = New ProcessStartInfo()



        pinfo.Arguments = para
        pinfo.FileName = TTS

        pinfo.CreateNoWindow = False
        pinfo.UseShellExecute = False

        p.StartInfo = pinfo
        p.Start()

        Do Until p.HasExited = True

            System.Threading.Thread.Sleep(50)
            If (x = 2400) Then
                Exit Do
            End If
        Loop


        '檢測是否已生成M3U8文件
        If System.IO.File.Exists(KVL) <> True Then
            'context.Response.Write(m3u8Path)
            'context.Response.End()
            Insert_Log(output)
        Else

            Try
                SQL = ""
                SQL &= " Declare  @code varchar(15) ='" & KCL & "' "
                SQL &= " Declare  @number int = " & VCR & " "
                SQL &= " "
                SQL &= ""
                SQL &= " UPDATE EL_MP4 "
                SQL &= " SET complete = 'Y',uploadtime = GETDATE() "
                SQL &= " WHERE 1=1  and number =  @number and code = @code "


                Cmd.CommandText = SQL
                Cmd.ExecuteNonQuery()

            Catch ex As Exception
                Insert_Log(ex.Message)
            End Try
            Insert_Log("Success")
        End If
        dote = True
    End Sub
#End Region


#Region "日誌"
    Sub Insert_Log(ByVal Msg As String)

        Dim sSource As String
        Dim sLog As String
        sSource = "EL_MP.4"
        sLog = "Application"

        If Not EventLog.SourceExists(sSource) Then
            EventLog.CreateEventSource(sSource, sLog)
        End If

        EventLog.WriteEntry(sSource, Msg)
    End Sub
End Class
#End Region








