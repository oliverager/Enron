"use client"

import type React from "react"

import { useState, useEffect } from "react"
import { Search, Download, Mail, ChevronLeft, ChevronRight, RefreshCw } from "lucide-react"
import { Input } from "../components/ui/input"
import { Button } from "../components/ui/button"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "../components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "../components/ui/table"
import { Skeleton } from "../components/ui/skeleton"
import { ThemeToggle } from "../components/theme-toggle"

// Interface matching the actual email data structure from the database
interface EmailResult {
  _id: string
  messageId: string
  date: string
  from: string
  to: string[] // Updated to be an array
  cc: string[]
  bcc: string[]
  subject: string
  body: string
  processedAt: {
    $date: string
  }
  indexed: boolean
}

export default function Home() {
  const [searchQuery, setSearchQuery] = useState("")
  const [isSearching, setIsSearching] = useState(false)
  const [results, setResults] = useState<EmailResult[]>([])
  const [hasSearched, setHasSearched] = useState(false)
  const [totalResults, setTotalResults] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const [mounted, setMounted] = useState(false)

  // Pagination state
  const [currentPage, setCurrentPage] = useState(1)
  const pageSize = 100 // Fixed page size of 100
  const [isChangingPage, setIsChangingPage] = useState(false)

  // Fix hydration issues by only rendering after component is mounted
  useEffect(() => {
    setMounted(true)
  }, [])

  const API_BASE_URL = "http://localhost:5284/api"

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!searchQuery.trim()) return

    setIsSearching(true)
    setHasSearched(true)
    setError(null)
    setCurrentPage(1) // Reset to first page on new search

    try {
      // Connect to the actual API endpoint with fixed page size of 100
      const response = await fetch(
        `${API_BASE_URL}/search?query=${encodeURIComponent(searchQuery)}&page=1&pageSize=${pageSize}`,
      )

      if (!response.ok) {
        throw new Error(`Search failed with status: ${response.status}`)
      }

      const data = await response.json()

      // Handle the response based on your API's actual structure
      if (Array.isArray(data)) {
        setResults(data)
        setTotalResults(data.length)
      } else if (data.hits && Array.isArray(data.hits)) {
        setResults(data.hits)
        setTotalResults(data.total || data.hits.length)
      } else {
        setResults([])
        setTotalResults(0)
        setError("Unexpected response format from API")
      }
    } catch (error) {
      console.error("Search error:", error)
      setResults([])
      setTotalResults(0)
      setError(`Failed to search: ${error instanceof Error ? error.message : "Unknown error"}`)
    } finally {
      setIsSearching(false)
    }
  }

  const changePage = async (newPage: number) => {
    if (newPage < 1 || newPage > Math.ceil(totalResults / pageSize)) return
    if (isChangingPage) return

    setIsChangingPage(true)

    try {
      console.log(`Fetching page ${newPage} with pageSize ${pageSize}`)
      const response = await fetch(
        `${API_BASE_URL}/search?query=${encodeURIComponent(searchQuery)}&page=${newPage}&pageSize=${pageSize}`,
      )

      if (!response.ok) {
        throw new Error(`Failed to fetch page ${newPage}`)
      }

      const data = await response.json()

      if (Array.isArray(data)) {
        setResults(data)
      } else if (data.hits && Array.isArray(data.hits)) {
        setResults(data.hits)
      } else {
        throw new Error("Unexpected response format")
      }

      // Only update the page number after successfully fetching the data
      setCurrentPage(newPage)
    } catch (error) {
      console.error("Page change error:", error)
      setError(`Failed to load page ${newPage}: ${error instanceof Error ? error.message : "Unknown error"}`)
    } finally {
      setIsChangingPage(false)
    }
  }

  const downloadEmail = async (messageId: string) => {
    try {
      // Assuming there's a download endpoint that accepts messageId
      const response = await fetch(`${API_BASE_URL}/download?messageId=${encodeURIComponent(messageId)}`)

      if (!response.ok) {
        throw new Error(`Download failed with status: ${response.status}`)
      }

      const blob = await response.blob()
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement("a")
      a.href = url
      a.download = `email-${messageId.substring(1, 10).replace(/[<>]/g, "")}.txt`
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)
    } catch (error) {
      console.error("Download error:", error)
      alert("Failed to download email. Please try again later.")
    }
  }

  // Format date to match email format: 'Fri, 30 Jun 2000 06:41:00 -0700'
  const formatDate = (dateStr: string): string => {
    if (!dateStr || dateStr === "Invalid Date") return "Unknown date"

    try {
      const date = new Date(dateStr)
      if (isNaN(date.getTime())) return "Unknown date"

      return date
        .toLocaleString("en-US", {
          weekday: "short", // "Fri"
          day: "2-digit", // "30"
          month: "short", // "Jun"
          year: "numeric", // "2000"
          hour: "2-digit", // "06"
          minute: "2-digit", // "41"
          second: "2-digit", // "00"
          timeZoneName: "short", // "-0700"
        })
        .replace(",", "") // Removes the comma after the weekday
    } catch {
      return "Unknown date"
    }
  }

  // Get excerpt from email body
  const getExcerpt = (body: string, maxLength = 100) => {
    if (!body) return "No content"
    return body.length > maxLength ? `${body.substring(0, maxLength)}...` : body
  }

  // Highlight search terms in text
  const highlightSearchTerms = (text: string) => {
    if (!searchQuery.trim() || !text) return text

    const terms = searchQuery.trim().toLowerCase().split(/\s+/)
    let highlightedText = text

    terms.forEach((term) => {
      if (term.length < 3) return // Skip very short terms

      const regex = new RegExp(`(${term})`, "gi")
      highlightedText = highlightedText.replace(regex, "<mark>$1</mark>")
    })

    return highlightedText
  }

  // Calculate pagination info
  const startIndex = (currentPage - 1) * pageSize + 1
  const endIndex = Math.min(currentPage * pageSize, totalResults)
  const totalPages = Math.ceil(totalResults / pageSize)

  // If not mounted yet, return a minimal UI to prevent hydration errors
  if (!mounted) {
    return <div className="min-h-screen bg-background"></div>
  }

  return (
    <div className="flex flex-col min-h-screen">
      <header className="border-b">
        <div className="container mx-auto py-6 px-4 flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold flex items-center gap-2">
              <Mail className="h-6 w-6" />
              Enron Email Search
            </h1>
            <p className="text-muted-foreground">Search through the Enron email dataset</p>
          </div>
          <ThemeToggle />
        </div>
      </header>

      <main className="flex-1 container mx-auto py-8 px-4">
        <Card className="mb-8">
          <CardHeader>
            <CardTitle>Search Emails</CardTitle>
            <CardDescription>Enter keywords to search through the Enron email dataset</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSearch} className="flex gap-2">
              <div className="relative flex-1">
                <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  type="search"
                  placeholder="Search by keywords, sender, or content..."
                  className="pl-8"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                />
              </div>
              <Button type="submit" disabled={isSearching}>
                {isSearching ? "Searching..." : "Search"}
              </Button>
            </form>
          </CardContent>
        </Card>

        {error && (
          <Card className="mb-8 border-destructive">
            <CardHeader className="text-destructive">
              <CardTitle>Error</CardTitle>
              <CardDescription>{error}</CardDescription>
            </CardHeader>
          </Card>
        )}

        {hasSearched && (
          <Card>
            <CardHeader>
              <CardTitle>Search Results</CardTitle>
              <CardDescription>
                {isSearching
                  ? "Searching..."
                  : results.length > 0
                    ? `Found ${totalResults} emails matching "${searchQuery}"`
                    : `No emails found matching "${searchQuery}"`}
              </CardDescription>
            </CardHeader>
            <CardContent>
              {isSearching || isChangingPage ? (
                // Loading state
                <div className="space-y-4">
                  {[1, 2, 3].map((i) => (
                    <div key={i} className="space-y-2">
                      <Skeleton className="h-5 w-2/3" />
                      <Skeleton className="h-4 w-full" />
                      <Skeleton className="h-4 w-4/5" />
                    </div>
                  ))}
                </div>
              ) : results.length > 0 ? (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-[40%]">Subject & Content</TableHead>
                      <TableHead className="w-[20%]">From</TableHead>
                      <TableHead className="w-[20%]">To</TableHead>
                      <TableHead className="w-[10%]">Date</TableHead>
                      <TableHead className="w-[10%]">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {results.slice(startIndex, endIndex).map((email) => (
                      <TableRow key={email._id}>
                        <TableCell>
                          <div className="font-medium">{email.subject || "(No Subject)"}</div>
                          <div
                            className="text-sm text-muted-foreground"
                            dangerouslySetInnerHTML={{
                              __html: highlightSearchTerms(getExcerpt(email.body)),
                            }}
                          />
                        </TableCell>
                        <TableCell className="truncate" title={email.from}>
                          {email.from}
                        </TableCell>
                        <TableCell className="max-w-[200px]">
                          <div className="space-y-1">
                            {Array.isArray(email.to) && email.to.length > 0 ? (
                              email.to.map((recipient, i) => (
                                <div key={i} className="truncate" title={recipient}>
                                  {recipient}
                                </div>
                              ))
                            ) : (
                              <div className="text-muted-foreground italic">(No recipients)</div>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>{formatDate(email.date)}</TableCell>
                        <TableCell>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => downloadEmail(email.messageId)}
                            title="Download email"
                          >
                            <Download className="h-4 w-4" />
                            <span className="sr-only">Download</span>
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              ) : (
                <div className="text-center py-8 text-muted-foreground">
                  No emails found matching your search criteria.
                </div>
              )}
            </CardContent>
            {results.length > 0 && (
              <CardFooter className="flex flex-col sm:flex-row justify-between items-center gap-4 pt-6">
                <div className="flex items-center gap-2">
                  <span className="text-sm text-muted-foreground whitespace-nowrap">
                    {startIndex} - {endIndex} of {totalResults}
                  </span>
                </div>

                <div className="flex items-center gap-1">
                  <Button
                    variant="outline"
                    size="icon"
                    onClick={() => changePage(currentPage - 1)}
                    disabled={currentPage === 1 || isChangingPage}
                    title="Previous page"
                  >
                    <ChevronLeft className="h-4 w-4" />
                  </Button>

                  <div className="flex items-center mx-2">
                    <span className="text-sm">
                      Page {currentPage} of {totalPages}
                    </span>
                  </div>

                  <Button
                    variant="outline"
                    size="icon"
                    onClick={() => changePage(currentPage + 1)}
                    disabled={currentPage >= totalPages || isChangingPage}
                    title="Next page"
                  >
                    <ChevronRight className="h-4 w-4" />
                  </Button>

                  {isChangingPage && <RefreshCw className="h-4 w-4 animate-spin ml-2" />}
                </div>
              </CardFooter>
            )}
          </Card>
        )}
      </main>

      <footer className="border-t py-6">
        <div className="container mx-auto text-center text-sm text-muted-foreground">
          Enron Email Dataset Search Tool &copy; {new Date().getFullYear()}
        </div>
      </footer>
    </div>
  )
}

