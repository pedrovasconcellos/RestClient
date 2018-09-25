Imports System.IO
Imports System.Net
Imports System.Net.Security
Imports System.Text

<Serializable>
Public Class RestClient

    Public Enum HttpVerb
        [GET] = 1
        [POST] = 2
        [PUT] = 3
        [DELETE] = 4
    End Enum
    Public Enum AuthorizationType
        [NoAuthorization] = 1
        [Basic] = 2
        [Token] = 3
    End Enum

    Public Class RestParametro
        Public Sub New()
        End Sub

        Public Property Campo As String
        Public Property Valor As String
    End Class

    Private _statusCode As HttpStatusCode
    Private _exception As Exception
    Private _message As String

    Public Property EndPoint As String
    Public Property Resource As String
    Public Property Method As HttpVerb
    Public Property ContentType As String
    Public Property PostData As String
    Public Property Authorization As AuthorizationType
    Public Property UserName As String
    Public Property Password As String
    Public Property Token As String
    Public Property StatusCode As HttpStatusCode
        Get
            Return _statusCode
        End Get
        Private Set
            _statusCode = Value
        End Set
    End Property
    Public Property Exception As Exception
        Get
            Return _exception
        End Get
        Private Set
            _exception = Value
        End Set
    End Property
    Public Property Message As String
        Get
            Return _message
        End Get
        Private Set
            _message = Value
        End Set
    End Property

    Public Sub New()
        Me.EndPoint = ""
        Me.Method = HttpVerb.GET
        Me.ContentType = "application/json"
        Me.PostData = ""
        Me.UserName = ""
        Me.Password = ""
        Me.Authorization = AuthorizationType.NoAuthorization
    End Sub

    Public Sub New(endpoint As String)
        Me.EndPoint = endpoint
        Me.Method = HttpVerb.GET
        Me.ContentType = "application/json"
        Me.PostData = ""
        Me.UserName = ""
        Me.Password = ""
        Me.Authorization = AuthorizationType.NoAuthorization
    End Sub

    Public Sub New(endpoint As String, method As HttpVerb)
        Me.EndPoint = endpoint
        Me.Method = method
        Me.ContentType = "application/json"
        Me.PostData = ""
        Me.UserName = ""
        Me.Password = ""
        Me.Authorization = AuthorizationType.NoAuthorization
    End Sub

    Public Sub New(endpoint As String, method As HttpVerb, postData As String)
        Me.EndPoint = endpoint
        Me.Method = method
        Me.ContentType = "application/json"
        Me.PostData = postData
        Me.UserName = ""
        Me.Password = ""
        Me.Authorization = AuthorizationType.NoAuthorization
    End Sub

    Public Sub New(endpoint As String, method As HttpVerb, postData As String, userName As String, password As String, authorization As AuthorizationType)
        Me.EndPoint = endpoint
        Me.Method = method
        Me.ContentType = "application/json"
        Me.PostData = postData
        Me.UserName = userName
        Me.Password = password
        Me.Authorization = authorization
    End Sub

    Public Sub New(endpoint As String, method As HttpVerb, authorization As AuthorizationType, token As String)
        Me.EndPoint = endpoint
        Me.Method = method
        Me.ContentType = "application/json"
        Me.Authorization = authorization
        Me.Token = token
    End Sub

    Public Sub New(endpoint As String, method As HttpVerb, authorization As AuthorizationType, token As String, postData As String)
        Me.EndPoint = endpoint
        Me.Method = method
        Me.ContentType = "application/json"
        Me.Authorization = authorization
        Me.Token = token
        Me.PostData = postData
    End Sub

    Public Function FazerRequisicao() As String
        Return FazerRequisicao("")
    End Function

    Public Function FazerRequisicao(parametros As String) As String
        Dim blnRetorno As Boolean = False
        Dim requisicao As HttpWebRequest = Nothing
        Dim responseValue As String = String.Empty
        Dim strErro As String = String.Empty
        Dim encryptString As String = String.Empty
        Dim responseResult As String = String.Empty

        Try

            requisicao = WebRequest.Create(Me.EndPoint + Me.Resource + parametros)
            Select Case Authorization
                Case AuthorizationType.Basic
                    encryptString = Convert.ToBase64String(Me.EncriptarString(String.Format("{0}:{1}", Me.UserName, Me.Password), Encoding.Default))
                    requisicao.Headers("Authorization") = String.Format("{0} {1}", Authorization.ToString(), encryptString)
                    Exit Select
                Case AuthorizationType.Token
                    requisicao.Headers("Authorization") = Me.Token
                    Exit Select
            End Select

            requisicao.Method = Me.Method.ToString()
            requisicao.ContentLength = 0
            requisicao.ContentType = Me.ContentType

            requisicao.KeepAlive = False
            ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf AcceptAllCertifications)

            If Not String.IsNullOrEmpty(Me.PostData) AndAlso (Me.Method = HttpVerb.POST Or Me.Method = HttpVerb.PUT) Then
                Dim bytes As Byte() = Encoding.GetEncoding("utf-8").GetBytes(PostData)
                requisicao.ContentLength = bytes.Length

                Using writeStream = requisicao.GetRequestStream()
                    writeStream.Write(bytes, 0, bytes.Length)
                End Using
            End If

            Using response As HttpWebResponse = requisicao.GetResponse()
                Me.StatusCode = response.StatusCode
                If response.StatusCode <> HttpStatusCode.OK Then
                    Dim message = String.Format("Requisição falhou. HTTP {0}", response.StatusCode)
                    Me.Exception = New Exception(message)
                End If

                Using responseStream = response.GetResponseStream()
                    If responseStream IsNot Nothing Then
                        Using reader = New StreamReader(responseStream)
                            responseValue = reader.ReadToEnd()
                        End Using
                    End If
                End Using
                blnRetorno = True
            End Using
            Return responseValue

        Catch ex As WebException
            Me.StatusCode = ex.Status
            Me.Exception = ex
            Me.Message = ex.Message
            Dim httpWebResponse = DirectCast(ex.Response, System.Net.HttpWebResponse)
            If (httpWebResponse.StatusCode = HttpStatusCode.BadRequest) Then
                Return New StreamReader(ex.Response.GetResponseStream()).ReadToEnd
            ElseIf (httpWebResponse.StatusCode = HttpStatusCode.NotFound) Then
                Return Nothing
            Else
                Throw ex
            End If
        Catch ex As Exception
            Me.StatusCode = HttpStatusCode.BadRequest
            Me.Exception = ex
            Me.Message = ex.Message
            Throw ex
        End Try
    End Function

    Private Function EncriptarString(dado As String, encode As Encoding) As Byte()
        Return encode.GetBytes(dado)
    End Function

    Public Function MontarParametro(ByVal parametro As List(Of RestParametro)) As String
        Dim retorno As String = String.Empty
        For Each item As RestParametro In parametro
            retorno = retorno + String.Format("{0}={1}&", item.Campo, item.Valor)
        Next
        retorno = retorno.Substring(0, retorno.Length - 1)
        Return retorno
    End Function

    Public Function AcceptAllCertifications(ByVal sender As Object, ByVal certification As System.Security.Cryptography.X509Certificates.X509Certificate, ByVal chain As System.Security.Cryptography.X509Certificates.X509Chain, ByVal sslPolicyErrors As System.Net.Security.SslPolicyErrors) As Boolean
        Return True
    End Function

End Class