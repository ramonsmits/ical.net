using System;
using System.Linq;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NUnit.Framework;

namespace Ical.Net.Tests;

[TestFixture]
public class OccurrenceIssue
{
    [Test, Explicit]
    public void MissingOccurrences()
    {
        var cal = Calendar.Load("""
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//Test//EN
            BEGIN:VEVENT
            DTSTART;VALUE=DATE:20251103
            DTEND;VALUE=DATE:20251124
            RRULE:FREQ=WEEKLY;WKST=MO;INTERVAL=48;BYDAY=MO
            UID:test-uid@example.com
            SUMMARY:Master Event
            END:VEVENT
            BEGIN:VEVENT
            DTSTART;VALUE=DATE:20251103
            DTEND;VALUE=DATE:20251124
            UID:test-uid@example.com
            RECURRENCE-ID;VALUE=DATE:20251103
            SEQUENCE:1
            SUMMARY:Override Event
            END:VEVENT
            END:VCALENDAR
            """)!;

        Console.WriteLine("Events:");
        foreach (var e in cal.Events.OrderBy(x=>x.Start))
        {
            Console.WriteLine($"\t{e.Uid.Substring(0,7)} {e.Start.Value} {e.End.Value} {e.Summary}");
        }

        var occurrences = cal
            .GetOccurrences<CalendarEvent>(
                new DateTime(2025, 1, 1),
                new DateTime(2027, 1, 1)
            )
            .OrderBy(e => e.Period.StartTime)
            .ToList();


        Console.WriteLine("Occurrences:");
        foreach (var o in occurrences)
        {
            var e = (CalendarEvent)o.Source;
            Console.WriteLine($"\t{e.Uid.Substring(0,7)} {o.Period.StartTime.Value} {o.Period.EndTime.Value} {e.Summary}");
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(occurrences, Has.Count.EqualTo(2));
            // The first occurrence should be the overridden one
            Assert.That(occurrences[0].Period.StartTime, Is.EqualTo(new CalDateTime(2025, 11,3)));
            Assert.That(occurrences[0].Source.RecurrenceId, Is.EqualTo(new CalDateTime(2025, 11, 3)));
            Assert.That(occurrences[1].Period.StartTime, Is.EqualTo(new CalDateTime(2026, 10, 5)));
            // Not sure why the following is null, in v5 it is not null
            Assert.That(occurrences[1].Source.RecurrenceId, Is.Null);
            Assert.That(((CalendarEvent) occurrences[0].Source).Summary, Is.EqualTo("Override Event"));
        }
    }
}
