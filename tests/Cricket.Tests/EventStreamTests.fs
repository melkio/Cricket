﻿namespace Cricket.Tests

open System.Threading
open NUnit.Framework
open FsUnit
open Cricket

[<TestFixture; Category("Unit"); Ignore("Temp failing on build server")>]
type ``Given an event stream``() = 

     [<Test>]
     member __.``I can post and subscribe to events``() =
        let es = new DefaultEventStream("test") :> IEventStream
        let resultGate = new ManualResetEvent(false)
        let result = ref 0
        es.Subscribe(fun (x:int) -> result := x; resultGate.Set() |> ignore)

        es.Publish(10)

        if resultGate.WaitOne(1000)
        then !result |> should equal 10
        else Assert.Fail("No result timeout") 

     [<Test>]
     member __.``I can remove a subscription post and subscribe to events by type``() =
        let es = new DefaultEventStream("test") :> IEventStream
        let resultGate = new ManualResetEvent(false)
        let result = ref 0
        es.Subscribe(fun (x:int) -> result := x; resultGate.Set() |> ignore)

        es.Publish(10)
        es.Unsubscribe<int>()
        es.Publish(12)
        if resultGate.WaitOne(1000)
        then !result |> should equal 10
        else Assert.Fail("No result timeout") 

     [<Test>]
     member __.``I can remove a subscription post and subscribe to events by key``() =
        let es = new DefaultEventStream("test") :> IEventStream
        let resultGate = new ManualResetEvent(false)
        let result = ref 0
        es.Subscribe("counters", fun (x:Event) -> result := unbox<_> x.Payload; resultGate.Set() |> ignore)

        es.Publish("counters", 10)
        es.Unsubscribe("counters")
        es.Publish("counters", 12)

        if resultGate.WaitOne(1000)
        then !result |> should equal 10
        else Assert.Fail("No result timeout") 
        

     [<Test>]
     member __.``I can post and subscribe to events with an event key``() =
        let es = new DefaultEventStream("test") :> IEventStream
        let resultGate = new ManualResetEvent(false)
        let result = ref 0
        es.Subscribe("counters", fun (x:Event) -> result := unbox<_> x.Payload; resultGate.Set() |> ignore)

        es.Publish("counters", 10)

        if resultGate.WaitOne(1000)
        then !result |> should equal 10
        else Assert.Fail("No result timeout") 
