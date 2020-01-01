namespace com.frannuca.network
open System
open System.Text.RegularExpressions
open HtmlAgilityPack
open System.Web

module MTLogger=
    let private cleanHtml (n:HtmlNode) =
        let html = HttpUtility.HtmlDecode(n.InnerHtml)
        if not(String.IsNullOrEmpty(html)) then
            Regex.Replace(html,"<.*?>|\[d+\]",String.Empty)

        else
            String.Empty
    let GetHTMLString (doc:HtmlDocument option seq)=
        doc
        |> Seq.map(fun node -> 
                            node
                            |> Option.map(fun (x:HtmlDocument) -> 
                                            try
                                                x.DocumentNode.SelectNodes("//title[normalize-space(text())]|//p[normalize-space(text())]|//h1[normalize-space(text())]") 
                                                |> Array.ofSeq
                                            with
                                            |  _ -> 
                                                    [||]
                                            )
                            |> Option.defaultValue([||])
                    )
        |> Seq.collect(id)
        |> Seq.map(fun n -> cleanHtml(n)+ "\n")
        |> Seq.fold(fun a b -> a + b) ""

    type LogMessage=
        |WriteMsg of string
        |PlotProgress of string
        |Stop 

    type ActorLogger(path:string)=
        let file = new System.IO.StreamWriter(path)

        let  _mailbox =
            MailboxProcessor.Start(fun inbox ->
                let rec loop() = async { 
                            let! msg = inbox.Receive()
                            match msg with
                            |WriteMsg(msg) -> file.WriteLine(msg)
                                              return! loop()
                            |PlotProgress(p) ->
                                              printfn "%s" p
                                              return! loop()
                                              
                            |Stop -> printfn "Leaving mailbox"
                                     return ()

                           }
                loop()
               )
        interface System.IDisposable with
            override self.Dispose()=
                Stop |> _mailbox.Post
                file.Close()

        member self.ProcessLogMessage(msg:string)=
             WriteMsg(msg) |> _mailbox.Post

        member self.ReportProgress(p:string)=
             PlotProgress(p) |> _mailbox.Post