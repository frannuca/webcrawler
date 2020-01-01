// Learn more about F# at http://fsharp.org

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Net
open System.Text.RegularExpressions
open com.frannuca.network.Craw
open System.Net.Http
open System;
open System.Collections.Generic;
open System.Linq;
open System.Text;
open System.Threading.Tasks;
open Catalyst;
open Catalyst.Models;
open Microsoft.Extensions.Logging;
open Mosaik.Core;

[<EntryPoint>]
let main argv =

    // Example:
    //crawl "http://news.google.com" 25
    //System.Threading.Thread.Sleep 1000000
   let options = argv |> List.ofArray

   let rec parseoptions(d:System.Collections.Generic.IDictionary<string,string>)(lst:string list)=
       match lst with
        |"-o"::x::t | "--output"::x::t -> 
                                            d.Add("output",x)
                                            parseoptions d t
        |"-l"::x::t | "--logfile"::x::t -> 
                                            d.Add("log",x)
                                            parseoptions d t
        |"-s"::x::t | "--stopwords"::x::t -> 
                                            d.Add("stopwords",x)
                                            parseoptions d t
        |"-u"::x::t | "--url"::x::t -> 
                                            d.Add("url",x)
                                            parseoptions d t
        |"-x"::x::t | "--maxlevel"::x::t -> 
                                            d.Add("maxlevel",x)
                                            parseoptions d t
        |"--help"::_ -> 
                        let sw = new StringWriter()
                        fprintfn sw "Options are:"
                        fprintfn sw "[-o | --output] pathToOutputTextFile"
                        fprintfn sw "[-x | --maxlevel] maxdepth"
                        fprintfn sw "[-u | --url]    urlpath"
                        fprintfn sw "[-s | --stopwords]    pathToInputStopwordsFile"
                        printfn "%s" (sw.ToString())

        |_ -> ()


   let dopt = new System.Collections.Generic.Dictionary<string,string>()
   parseoptions dopt options

   let path2dump =
                if dopt.ContainsKey("output") then
                    dopt.["output"]
                else
                    "dump.txt"
   let path2log =
                if dopt.ContainsKey("log") then
                    dopt.["log"]
                else
                    "dump.txt.log"
   let stopwords =
                if dopt.ContainsKey("stopwords") then
                    dopt.["stopwords"]
                else
                    "resources/stopwords_en.txt"
   let rooturl = 
                if dopt.ContainsKey("url") then
                    dopt.["url"]
                else
                    failwith "Invalid URL"
   let mxlevel = 
                 if dopt.ContainsKey("maxlevel") then
                     System.Convert.ToInt32(dopt.["maxlevel"])
                 else
                     0

   
   let crawlerobj = new Crawler(mxlevel,25,stopwords, path2log, path2dump)
   let tree = crawlerobj.Crawl(rooturl)
   printfn "%A" tree



   tree.Serialize2Disk(path2dump+".final")


   0 // return an integer exit code
