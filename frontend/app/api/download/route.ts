export async function GET(request: Request) {
  const { searchParams } = new URL(request.url)
  const messageId = searchParams.get("messageId")

  if (!messageId) {
    return new Response("Missing messageId parameter", { status: 400 })
  }

  try {
    // In a real implementation, you would fetch the full email content from your database
    // Mock email content for demonstration
    const mockEmail = `From: phillip.allen@enron.com
To: leah.arsdall@enron.com
Subject: Re: test
Date: (Unknown)
Message-ID: ${messageId}

test successful. way to go!!!

---
This email was retrieved from the Enron Email Dataset.
`

    // Return the email content as a downloadable text file
    return new Response(mockEmail, {
      headers: {
        "Content-Type": "text/plain",
        "Content-Disposition": `attachment; filename="email-${messageId.substring(1, 10)}.txt"`,
      },
    })
  } catch (error) {
    console.error("Download error:", error)
    return new Response("Failed to download email", { status: 500 })
  }
}

