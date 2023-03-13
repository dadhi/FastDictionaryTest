﻿namespace FastDictionaryTest.FibonacciHashing

open System.Collections.Generic
open System.Numerics

module private Helpers =

    type Entry<'Key, 'Value> =
        {
            Key: 'Key
            Value: 'Value
        }

open Helpers


type Dictionary<'Key, 'Value when 'Key : equality> (entries: seq<'Key * 'Value>) =
    // Track the number of items in Dictionary for resize
    let mutable count = 0
    // Create the Buckets with some initial capacity
    let mutable buckets : list<Entry<'Key, 'Value>>[] = Array.create 4 []
    // BitShift necessary for mapping HashCode to SlotIdx using Fibonacci Hashing
    let mutable slotBitShift = 32 - (BitOperations.TrailingZeroCount buckets.Length)

    // This relies on the size of buckets being a power of 2
    let computeBucketIndex (key: 'Key) =
        let h = (EqualityComparer.Default.GetHashCode key)
        let hashProduct = (uint h) * 2654435769u
        int (hashProduct >>> slotBitShift)


    let addEntry (key: 'Key) (value: 'Value) =

        let rec loop (acc: Entry<_,_> list) (remaining: Entry<_,_> list) =
            match remaining with
            | [] ->
                let newEntry = { Key = key; Value = value }
                // Increment the count since we have added an entry
                count <- count + 1
                newEntry :: acc
            | head::tail ->
                if EqualityComparer<'Key>.Default.Equals (head.Key, key) then
                    // Do not increment the count in this case because we are just overwriting an
                    // exising value
                    let updatedEntry = { head with Value = value }
                    (updatedEntry::acc) @ tail
                else
                    loop (head::acc) tail

        let bucketIdx = computeBucketIndex key
        let bucket = buckets[bucketIdx]
        let updatedBucket = loop [] bucket
        buckets[bucketIdx] <- updatedBucket


    let getValue (key: 'Key) =

        let rec loop (entry: Entry<_,_> list) =
            match entry with
            | [] ->
                raise (KeyNotFoundException())
            | head::tail ->
                if EqualityComparer.Default.Equals (head.Key, key) then
                    head.Value
                else
                    loop tail

        let bucketIdx = computeBucketIndex key
        loop buckets[bucketIdx]


    let resize () =
        // Only resize when the fill is > 75%
        if count > (buckets.Length >>> 2) * 3 then
            let oldBuckets = buckets

            // Increase the size of the backing store
            buckets <- Array.create (buckets.Length <<< 1) []
            slotBitShift <- 32 - (System.Numerics.BitOperations.TrailingZeroCount buckets.Length)
            count <- 0

            for bucket in oldBuckets do
                for entry in bucket do
                    addEntry entry.Key entry.Value

    do
        for k, v in entries do
            addEntry k v
            resize()

    new () = Dictionary([])

    member d.Item
        with get (key: 'Key) = getValue key
