import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Alert, Button, Card, Col, Container, Form, InputGroup, Row, Spinner } from 'react-bootstrap'
import { useAuth } from '../Context/AuthContext.jsx'

const Login = () => {
    const navigate = useNavigate()
    const { login } = useAuth()
    const [validated, setValidated] = useState(false)
    const [isSubmitting, setIsSubmitting] = useState(false)
    const [feedback, setFeedback] = useState({ type: '', message: '' })

    const handleSubmit = async (event) => {
        event.preventDefault()

        const form = event.currentTarget
        const formData = new FormData(form)
        const username = String(formData.get('username') ?? '').trim()
        const password = String(formData.get('password') ?? '')

        setValidated(true)
        setFeedback({ type: '', message: '' })

        if (!form.checkValidity()) {
            return
        }

        setIsSubmitting(true)

        try {
            await login({ username, password })
            form.reset()
            setValidated(false)
            navigate('/', { replace: true })
        } catch (error) {
            setFeedback({
                type: 'danger',
                message:
                    error instanceof Error
                        ? error.message
                        : 'Unable to complete login.',
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
                                    <h1 className="h3 fw-bold mb-2">Welcome back</h1>
                                    <p className="text-muted mb-0">
                                        Log in to continue to your workspace.
                                    </p>
                                </div>

                                {feedback.message && (
                                    <Alert variant={feedback.type} className="mb-3">
                                        {feedback.message}
                                    </Alert>
                                )}

                                <Form noValidate validated={validated} onSubmit={handleSubmit}>
                                    <Form.Group className="mb-3" controlId="loginUsername">
                                        <Form.Label column className="p-0 mb-2">
                                            Username
                                        </Form.Label>
                                        <InputGroup hasValidation>
                                            <Form.Control
                                                required
                                                type="text"
                                                name="username"
                                                placeholder="Jonh Doe"
                                            />
                                            <Form.Control.Feedback type="invalid">
                                                Enter your username.
                                            </Form.Control.Feedback>
                                        </InputGroup>
                                    </Form.Group>

                                    <Form.Group className="mb-4" controlId="loginPassword">
                                        <Form.Label column className="p-0 mb-2">
                                            Password
                                        </Form.Label>
                                        <Form.Control
                                            required
                                            type="password"
                                            name="password"
                                            placeholder="Your password"
                                        />
                                        <Form.Control.Feedback type="invalid">
                                            Enter your password.
                                        </Form.Control.Feedback>
                                    </Form.Group>

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
                                                Logging in...
                                            </>
                                        ) : (
                                            'Log in'
                                        )}
                                    </Button>
                                </Form>

                                <p className="text-center text-muted mt-4 mb-0">
                                    Don&apos;t have an account? <Link to="/register">Sign up</Link>
                                </p>
                            </Card.Body>
                        </Card>
                    </Col>
                </Row>
            </Container>
        </main>
    )
}

export default Login