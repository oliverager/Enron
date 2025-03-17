import { NextResponse } from "next/server"

// This is a placeholder API route that would connect to your Elasticsearch instance
export async function GET(request: Request) {
  const { searchParams } = new URL(request.url)
  const query = searchParams.get("q")

  if (!query) {
    return NextResponse.json({ hits: [], total: 0 })
  }

  try {
    // In a real implementation, you would connect to Elasticsearch here
    // This is mock data based on the provided example
    const mockEmailResult = {
      _id: "67d8602eb74290cc9004c43e",
      messageId: "<24216240.1075855687451.JavaMail.evans@thyme>",
      date: "Invalid Date",
      from: "phillip.allen@enron.com",
      to: "leah.arsdall@enron.com",
      cc: [],
      bcc: [],
      subject: "Re: test",
      body: "test successful. way to go!!!",
      processedAt: {
        $date: "2025-03-17T17:47:26.255Z",
      },
      indexed: true,
    }

    // Mock search results with variations
    const results = query.toLowerCase().includes("test")
      ? [
          mockEmailResult,
          {
            ...mockEmailResult,
            _id: "67d8602eb74290cc9004c43f",
            subject: "Testing the system",
            body: "This is another test email to verify the system is working correctly.",
            from: "jane.doe@enron.com",
            to: "john.smith@enron.com",
          },
          {
            ...mockEmailResult,
            _id: "67d8602eb74290cc9004c440",
            subject: "RE: Weekly Test Results",
            body: "Here are the test results from last week's analysis. Please review and provide feedback.",
            from: "robert.johnson@enron.com",
            to: "analytics@enron.com",
          },
        ]
      : []

    return NextResponse.json({
      hits: results,
      total: results.length,
    })
  } catch (error) {
    console.error("Search error:", error)
    return NextResponse.json({ error: "Search failed" }, { status: 500 })
  }
}

