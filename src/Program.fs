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

   let path = "/Users/fran/temp/dump.txt"
   let level = System.Convert.ToInt32 argv.[1]
   let crawlerobj = new Crawler(level,25,path)
   let tree = crawlerobj.Crawl(argv.[0])
   printfn "%A" tree



   //tree.Serialize2Disk(path)


   0 // return an integer exit code
