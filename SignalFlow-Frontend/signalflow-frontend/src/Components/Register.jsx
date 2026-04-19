import { useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Alert,
  Button,
  Card,
  Col,
  Container,
  Form,
  InputGroup,
  Row,
  Spinner,
} from 'react-bootstrap'
import { useAuth } from '../Context/AuthContext.jsx'

const Register = () => {
  const { register } = useAuth()
  const [validated, setValidated] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [feedback, setFeedback] = useState({ type: '', message: '' })

  const handleSubmit = async (event) => {
    event.preventDefault()

    const form = event.currentTarget
    const formData = new FormData(form)
    const username = String(formData.get('username') ?? '').trim()
    const email = String(formData.get('email') ?? '').trim()
    const password = String(formData.get('password') ?? '')
    const confirmPassword = String(formData.get('confirmPassword') ?? '')

    setValidated(true)
    setFeedback({ type: '', message: '' })

    if (password !== confirmPassword) {
      setFeedback({
        type: 'danger',
        message: 'Passwords do not match.',
      })
      return
    }

    if (!form.checkValidity()) {
      return
    }

    setIsSubmitting(true)

    try {
      const session = await register({ username, email, password })
      form.reset()
      setValidated(false)
      setFeedback({
        type: 'success',
        message:
         `Registration completed for ${session.username}.`,
      })
    } catch (error) {
      setFeedback({
        type: 'danger',
        message:
          error instanceof Error
            ? error.message
            : 'Unable to complete registration.',
      })
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="min-vh-100 d-flex align-items-center bg-body-tertiary py-5">
      <Container>
        <Row className="justify-content-center">
          <Col xs={12} md={10} lg={8} xl={6}>
            <Card className="border-0 shadow-lg rounded-4 overflow-hidden">
              <Card.Body className="p-4 p-md-5">
                <div className="text-center mb-4">
                  <p className="text-uppercase text-primary fw-semibold small mb-2">
                    SignalFlow
                  </p>
                  <h1 className="h3 fw-bold mb-2">Create your account</h1>
                  <p className="text-muted mb-0">
                    Sign up to access your dashboard and get started right away.
                  </p>
                </div>

                {feedback.message && (
                  <Alert variant={feedback.type} className="mb-3">
                    {feedback.message}
                  </Alert>
                )}

                <Form noValidate validated={validated} onSubmit={handleSubmit}>
                  <Form.Group className="mb-3" controlId="registerUsername">
                    <Form.Label column className="p-0 mb-2">
                      Username
                    </Form.Label>
                    <InputGroup hasValidation>
                      <Form.Control
                        required
                        type="text"
                        name="username"
                        minLength={3}
                        placeholder="Jonh Doe"
                      />
                      <Form.Control.Feedback type="invalid">
                        Choose a username with at least 3 characters.
                      </Form.Control.Feedback>
                    </InputGroup>
                  </Form.Group>

                  <Form.Group className="mb-3" controlId="registerEmail">
                    <Form.Label column className="p-0 mb-2">
                      Email
                    </Form.Label>
                    <Form.Control
                      required
                      type="email"
                      name="email"
                      placeholder="name@email.com"
                    />
                    <Form.Control.Feedback type="invalid">
                      Enter a valid email.
                    </Form.Control.Feedback>
                  </Form.Group>

                  <Row>
                    <Col md={6}>
                      <Form.Group className="mb-3" controlId="registerPassword">
                        <Form.Label column className="p-0 mb-2">
                          Password
                        </Form.Label>
                        <Form.Control
                          required
                          type="password"
                          name="password"
                          minLength={8}
                          placeholder="At least 8 characters"
                        />
                        <Form.Control.Feedback type="invalid">
                          Password must be at least 8 characters long.
                        </Form.Control.Feedback>
                      </Form.Group>
                    </Col>

                    <Col md={6}>
                      <Form.Group className="mb-3" controlId="registerConfirmPassword">
                        <Form.Label column className="p-0 mb-2">
                          Confirm password
                        </Form.Label>
                        <Form.Control
                          required
                          type="password"
                          name="confirmPassword"
                          placeholder="Repeat password"
                        />
                        <Form.Control.Feedback type="invalid">
                          Confirm your password.
                        </Form.Control.Feedback>
                      </Form.Group>
                    </Col>
                  </Row>

                  <Button
                    type="submit"
                    variant="primary"
                    className="w-100 py-2 fw-semibold"
                    disabled={isSubmitting}
                  >
                    {isSubmitting ? (
                      <>
                        <Spinner
                          as="span"
                          animation="border"
                          size="sm"
                          role="status"
                          aria-hidden="true"
                          className="me-2"
                        />
                        Registration in progress...
                      </>
                    ) : (
                      'Sign up'
                    )}
                  </Button>
                </Form>

                <p className="text-center text-muted mt-4 mb-0">
                  Already have an account? <Link to="/login">Log in</Link>
                </p>
              </Card.Body>
            </Card>
          </Col>
        </Row>
      </Container>
    </main>
  )
}

export default Register
