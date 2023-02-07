using System.Collections.Generic;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace Pong.AI
{
    /// <summary>
    /// Implementation of a feedforward neural network.
    /// </summary>
    public class NeuralNetwork
    {
        /// <summary>
        /// How many layers of neurons (3+). Do not use 1.
        /// 2 => input connected to output.
        /// 1 => input is output, and feed forward will crash.
        /// </summary>
        internal readonly int[] Layers;

        /// <summary>
        /// The neurons.
        /// [layer][neuron]
        /// </summary>
        internal double[][] Neurons;

        /// <summary>
        /// NN Biases. Either improves or lowers the chance of this neuron fully firing.
        /// [layer][neuron]
        /// </summary>
        private double[][] Biases;

        /// <summary>
        /// NN weights. Reduces or amplifies the output for the relationship between neurons in each layer
        /// [layer][neuron][neuron]
        /// </summary>
        private double[][][] Weights;

        #region BACK PROPAGATION
        /// <summary>
        /// 
        /// </summary>
        private readonly float learningRate = 0.01f;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="layerDefinition">Defines size of the layers.</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Init*() set the fields.
        internal NeuralNetwork(int[] layerDefinition)
#pragma warning restore CS8618
        {
            // (1) INPUT (2) HIDDEN (3) OUTPUT.
            if (layerDefinition.Length < 2) throw new ArgumentException(nameof(layerDefinition) + " insufficient layers.");

            // copy layerDefinition to Layers; although for cars this must not change.     
            Layers = new int[layerDefinition.Length];

            for (int layer = 0; layer < layerDefinition.Length; layer++)
            {
                Layers[layer] = layerDefinition[layer];
            }

            // if layerDefinition is [2,3,2] then...
            // 
            // Neurons :      (o) (o)    <-2  INPUT
            //              (o) (o) (o)  <-3
            //                (o) (o)    <-2  OUTPUT
            //

            InitialiseNeurons();
            InitialiseBiases();
            InitialiseWeights();
        }

        /// <summary>
        /// Tanh squashes a real-valued number to the range [-1, 1]. It’s non-linear. 
        /// But unlike Sigmoid, its output is zero-centered. Therefore, in practice the tanh non-linearity is always preferred 
        /// to the sigmoid nonlinearity.
        /// 
        /// Activate is TANH         1_       ___
        /// (hyperbolic tangent)     0_      /
        ///                         -1_  ___/
        ///                                | | |
        ///                     -infinity -2 0 2..infinity
        ///                               
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static double TanHActivationFunction(double value)
        {
            return (double)Math.Tanh(value);
        }

        /// <summary>
        /// Derivative (for back-propagation of TanH activation function).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double DerivativeOfTanHActivationFunction(double value)
        {
            return 1 - value;
        }

        /// <summary>
        /// LeakyReLU alpha.
        /// </summary>
        private const double alpha = 0.001f;

        /// <summary>
        /// The rectified linear activation function or ReLU for short is a piecewise linear function that will output 
        /// the input directly if it is positive, otherwise, it will output zero. It has become the default activation 
        /// function for many types of neural networks because a model that uses it is easier to train and often achieves 
        /// better performance.
        /// 
        /// Literally, if input <0 returns 0 else returns input. i.e. loses anything negative.
        /// 
        /// See: https://machinelearningmastery.com/rectified-linear-activation-function-for-deep-learning-neural-networks/#:~:text=The%20rectified%20linear%20activation%20function,otherwise%2C%20it%20will%20output%20zero.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static double PReLUActivationFunction(double input)
        {
            return input > 0 ? input : alpha * input;
        }

        /// <summary>
        /// Derivative (for back-propagation of PReLU).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static double DerivativeOfPReLUActivationFunction(double input)
        {
            return input > 0 ? 1 : alpha;
        }

        /// <summary>
        /// Create empty storage array for the neurons in the network.
        /// </summary>
        private void InitialiseNeurons()
        {
            List<double[]> neuronsList = new();

            // if layerDefinition is [2,3,2] ..   float[]
            // Neurons :      (o) (o)    <-2  ... [ 0, 0 ]
            //              (o) (o) (o)  <-3  ... [ 0, 0, 0 ]
            //                (o) (o)    <-2  ... [ 0, 0 ]
            //

            for (int layer = 0; layer < Layers.Length; layer++)
            {
                neuronsList.Add(new double[Layers[layer]]);
            }

            Neurons = neuronsList.ToArray();
        }

        /// <summary>
        /// Generate a cryptographic random number between -0.5...+0.5.
        /// </summary>
        /// <returns></returns>
        private static float RandomFloatBetweenMinusHalfToPlusHalf()
        {
            return (float)(RandomNumberGenerator.GetInt32(0, 10000) - 5000) / 10000;
        }

        /// <summary>
        /// initializes and populates biases.
        /// </summary>
        private void InitialiseBiases()
        {
            List<double[]> biasList = new();

            // for each layer of neurons, we have to set biases.
            for (int layer = 1; layer < Layers.Length; layer++)
            {
                double[] bias = new double[Layers[layer]];

                for (int biasLayer = 0; biasLayer < Layers[layer]; biasLayer++)
                {
                    bias[biasLayer] = RandomFloatBetweenMinusHalfToPlusHalf(); //0
                }

                biasList.Add(bias);
            }

            Biases = biasList.ToArray();
        }

        /// <summary>
        /// Initializes random array for the weights being held in the network.
        /// </summary>
        private void InitialiseWeights()
        {
            List<double[][]> weightsList = new(); // used to construct weights, as dynamic arrays aren't supported

            for (int layer = 1; layer < Layers.Length; layer++)
            {
                List<double[]> layerWeightsList = new();

                int neuronsInPreviousLayer = Layers[layer - 1];

                for (int neuronIndexInLayer = 0; neuronIndexInLayer < Neurons[layer].Length; neuronIndexInLayer++)
                {
                    double[] neuronWeights = new double[neuronsInPreviousLayer];

                    for (int neuronIndexInPreviousLayer = 0; neuronIndexInPreviousLayer < neuronsInPreviousLayer; neuronIndexInPreviousLayer++)
                    {
                        neuronWeights[neuronIndexInPreviousLayer] = RandomFloatBetweenMinusHalfToPlusHalf();
                    }

                    layerWeightsList.Add(neuronWeights);
                }

                weightsList.Add(layerWeightsList.ToArray());
            }

            Weights = weightsList.ToArray();
        }

        /// <summary>
        /// Feed forward, inputs >==> outputs.
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        internal unsafe double[] FeedForward(double[] inputs)
        {
            // put the INPUT values into layer 0 neurons
            for (int i = 0; i < inputs.Length; i++)
            {
                Neurons[0][i] = inputs[i];
            }

            // we start on layer 1 as we are computing values from prior layers (layer 0 is inputs)

            for (int layer = 1; layer < Layers.Length; layer++)
            {
                int layerminus1 = layer - 1;

                for (int neuronIndexForLayer = 0; neuronIndexForLayer < Layers[layer]; neuronIndexForLayer++)
                {
                    // sum of outputs from the previous layer
                    double value = 0f;

                    for (int neuronIndexInPreviousLayer = 0; neuronIndexInPreviousLayer < Layers[layerminus1]; neuronIndexInPreviousLayer++)
                    {
                        // remember: the "weight" amplifies or reduces, so we take the output of the prior neuron and "amplify/reduce" it's output here
                        value += Weights[layerminus1][neuronIndexForLayer][neuronIndexInPreviousLayer] * Neurons[layerminus1][neuronIndexInPreviousLayer];
                    }

                    // any neuron fires or not based on the input. The point of a bias is to move the activation up or down.
                    // e.g. the value could be 0.3, adding a bias of 0.5 takes it to 0.8. You might think why not just use the weights to achieve this
                    // but remember weights are individual per prior layer neurons, the bias affects the SUM() of them.

                    Neurons[layer][neuronIndexForLayer] = TanHActivationFunction(value + Biases[layerminus1][neuronIndexForLayer]);
                }
            }

            return Neurons[^1]; // final* layer contains OUTPUT
        }


        /// <summary>
        /// Back propagation.
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="expected"></param>
        public void BackPropagate(double[] inputs, double[] expected)
        {
            double[] output = FeedForward(inputs); // runs feed forward to ensure neurons are populated correctly

            double[][] gamma;

            List<double[]> gammaList = new();

            for (int i = 0; i < Layers.Length; i++)
            {
                gammaList.Add(new double[Layers[i]]);
            }

            gamma = gammaList.ToArray(); // gamma initialization

            for (int i = 0; i < output.Length; i++)
            {
                gamma[Layers.Length - 1][i] = (output[i] - expected[i]) * DerivativeOfTanHActivationFunction(output[i]); // Gamma calculation
            }

            for (int i = 0; i < Layers[^1]; i++) // calculates the w' and b' for the last layer in the network
            {
                Biases[Layers.Length - 2][i] -= gamma[Layers.Length - 1][i] * learningRate;

                for (int j = 0; j < Layers[^2]; j++)
                {
                    Weights[Layers.Length - 2][i][j] -= gamma[Layers.Length - 1][i] * Neurons[Layers.Length - 2][j] * learningRate; //*learning 

                }
            }

            for (int i = Layers.Length - 2; i > 0; i--) // runs on all hidden layers
            {         
                for (int j = 0; j < Layers[i]; j++) // outputs
                {
                    gamma[i][j] = 0;

                    for (int k = 0; k < gamma[i + 1].Length; k++)
                    {
                        gamma[i][j] += gamma[i + 1][k] * Weights[i][k][j];
                    }

                    gamma[i][j] *= DerivativeOfTanHActivationFunction(Neurons[i][j]); //calculate gamma
                }

                for (int j = 0; j < Layers[i]; j++) // iterate over outputs of layer
                {
                    Biases[i - 1][j] -= gamma[i][j] * learningRate; // modify biases of network

                    for (int k = 0; k < Layers[i - 1]; k++) // iterate over inputs to layer
                    {
                        Weights[i - 1][j][k] -= gamma[i][j] * Neurons[i - 1][k] * learningRate; // modify weights of network
                    }
                }
            }
        }

        /// <summary>
        /// Saves the biases and weights within the network to a file.
        /// </summary>
        /// <param name="path"></param>
        internal void Save(string path)
        {
            using StreamWriter writer = new(path, false);

            // write the biases
            for (int layerIndex = 0; layerIndex < Biases.Length; layerIndex++)
            {
                for (int neuronIndex = 0; neuronIndex < Biases[layerIndex].Length; neuronIndex++)
                {
                    writer.WriteLine(Biases[layerIndex][neuronIndex]);
                }
            }

            // write the weights
            for (int layerIndex = 0; layerIndex < Weights.Length; layerIndex++)
            {
                for (int neuronIndexInLayer = 0; neuronIndexInLayer < Weights[layerIndex].Length; neuronIndexInLayer++)
                {
                    for (int neuronIndexInPreviousLayer = 0; neuronIndexInPreviousLayer < Weights[layerIndex][neuronIndexInLayer].Length; neuronIndexInPreviousLayer++)
                    {
                        writer.WriteLine(Weights[layerIndex][neuronIndexInLayer][neuronIndexInPreviousLayer]);
                    }
                }
            }

            writer.Close();
        }

        /// <summary>
        /// This loads the biases and weights from within a file into the neural network.
        /// </summary>
        /// <param name="path"></param>
        internal bool Load(string path)
        {
            if (!File.Exists(path)) return false;

            string[] ListLines = File.ReadAllLines(path);

            int index = 0;

            try
            {

                for (int layerIndex = 0; layerIndex < Biases.Length; layerIndex++)
                {
                    for (int neuronIndex = 0; neuronIndex < Biases[layerIndex].Length; neuronIndex++)
                    {
                        Biases[layerIndex][neuronIndex] = double.Parse(ListLines[index++]);
                    }
                }

                for (int layerIndex = 0; layerIndex < Weights.Length; layerIndex++)
                {
                    for (int neuronIndexInLayer = 0; neuronIndexInLayer < Weights[layerIndex].Length; neuronIndexInLayer++)
                    {
                        for (int neuronIndexInPreviousLayer = 0; neuronIndexInPreviousLayer < Weights[layerIndex][neuronIndexInLayer].Length; neuronIndexInPreviousLayer++)
                        {
                            Weights[layerIndex][neuronIndexInLayer][neuronIndexInPreviousLayer] = double.Parse(ListLines[index++]);
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to load .AI files\nThe most likely reason is that the number of neurons does not match the saved AI file.");
                return false;
            }

            return true;
        }
    }
}